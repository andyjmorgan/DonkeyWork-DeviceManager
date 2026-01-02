// Notification DTOs matching backend contracts

export interface DeviceStatusNotification {
  deviceId: string;
  deviceName: string;
  online: boolean;
  timestamp: string;
  roomName?: string;
  buildingName?: string;
}
