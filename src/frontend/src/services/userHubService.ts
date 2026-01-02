import * as signalR from '@microsoft/signalr';
import type { DeviceStatusNotification } from '../types/notifications';

/**
 * SignalR hub connection manager for real-time notifications
 */
class UserHubService {
  private connection: signalR.HubConnection | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private isStarting = false;

  /**
   * Start the SignalR connection
   */
  async start(): Promise<void> {
    // If connection exists and is in a usable state, don't recreate
    if (this.connection) {
      const state = this.connection.state;
      if (state === signalR.HubConnectionState.Connected || state === signalR.HubConnectionState.Connecting) {
        console.log(`SignalR connection already ${state}`);
        return;
      }
      // If disconnected or disconnecting, stop and clear it first
      if (state === signalR.HubConnectionState.Disconnecting) {
        console.log('Waiting for existing connection to disconnect...');
        await this.stop();
      }
    }

    if (this.isStarting) {
      console.log('SignalR connection is already starting, skipping...');
      return;
    }

    this.isStarting = true;
    console.log('Starting new SignalR connection...');

    const accessToken = localStorage.getItem('access_token');
    if (!accessToken) {
      this.isStarting = false;
      throw new Error('No access token found');
    }

    // Create connection
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/user', {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 10s, 30s, 60s
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          if (retryContext.previousRetryCount === 3) return 30000;
          return 60000;
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Handle reconnection events
    this.connection.onreconnecting((error) => {
      console.warn('SignalR reconnecting...', error);
      this.reconnectAttempts++;
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected successfully', connectionId);
      this.reconnectAttempts = 0;
    });

    this.connection.onclose((error) => {
      console.error('SignalR connection closed', error);

      // Attempt to reconnect if under max attempts
      if (this.reconnectAttempts < this.maxReconnectAttempts) {
        setTimeout(() => {
          console.log('Attempting to restart SignalR connection...');
          this.start().catch((err) => {
            console.error('Failed to restart SignalR connection', err);
          });
        }, 5000);
      }
    });

    // Start connection
    try {
      await this.connection.start();
      console.log('SignalR connected successfully');
      this.reconnectAttempts = 0;
      this.isStarting = false;
    } catch (error) {
      console.error('Failed to start SignalR connection', error);
      this.isStarting = false;
      this.connection = null; // Clear connection on failure
      throw error;
    }
  }

  /**
   * Stop the SignalR connection
   */
  async stop(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('SignalR connection stopped');
      } catch (error) {
        console.error('Error stopping SignalR connection:', error);
      } finally {
        this.connection = null;
        this.isStarting = false;
      }
    }
  }

  /**
   * Subscribe to device status notifications
   * Must be called after start() to ensure connection exists
   */
  onDeviceStatus(callback: (notification: DeviceStatusNotification) => void): void {
    if (!this.connection) {
      console.error('SignalR connection not started - cannot subscribe to events');
      throw new Error('SignalR connection not started');
    }

    this.connection.on('ReceiveDeviceStatus', callback);
    console.log('Subscribed to ReceiveDeviceStatus events');
  }

  /**
   * Send ping command to a device
   */
  async pingDevice(deviceId: string): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR connection not started');
    }

    await this.connection.invoke('PingDevice', deviceId);
    console.log(`Ping command sent to device ${deviceId}`);
  }

  /**
   * Send shutdown command to a device
   */
  async shutdownDevice(deviceId: string): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR connection not started');
    }

    await this.connection.invoke('ShutdownDevice', deviceId);
    console.log(`Shutdown command sent to device ${deviceId}`);
  }

  /**
   * Send restart command to a device
   */
  async restartDevice(deviceId: string): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR connection not started');
    }

    await this.connection.invoke('RestartDevice', deviceId);
    console.log(`Restart command sent to device ${deviceId}`);
  }

  /**
   * Get connection state
   */
  getConnectionState(): signalR.HubConnectionState | null {
    return this.connection?.state ?? null;
  }

  /**
   * Check if connected
   */
  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

// Export singleton instance
export const userHubService = new UserHubService();
