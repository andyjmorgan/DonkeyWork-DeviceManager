import { useRef, useState, useEffect } from 'react';
import { Card } from 'primereact/card';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import { InputText } from 'primereact/inputtext';
import { Toast } from 'primereact/toast';
import { Dropdown } from 'primereact/dropdown';
import { ProgressSpinner } from 'primereact/progressspinner';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Paginator } from 'primereact/paginator';
import { Tag } from 'primereact/tag';
import { SplitButton } from 'primereact/splitbutton';
import { lookupDeviceRegistration, completeDeviceRegistration } from '../services/deviceRegistrationService';
import { getDevices, updateDeviceDescription, deleteDevice } from '../services/deviceService';
import { userHubService } from '../services/userHubService';
import type { DeviceRegistrationLookupDto, BuildingDto, RoomDto } from '../types/deviceRegistration';
import type { DeviceResponse, PaginatedResponse } from '../types/device';
import type { DeviceStatusNotification } from '../types/notifications';
import './Dashboard.css';

function Dashboard() {
  const toastRef = useRef<Toast>(null);
  const hubInitialized = useRef(false);
  const devicesLoadedForPage = useRef<number | null>(null);
  const [registerDialogVisible, setRegisterDialogVisible] = useState(false);
  const [deviceCode, setDeviceCode] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [lookupData, setLookupData] = useState<DeviceRegistrationLookupDto | null>(null);
  const [selectedBuilding, setSelectedBuilding] = useState<BuildingDto | null>(null);
  const [selectedRoom, setSelectedRoom] = useState<RoomDto | null>(null);

  // Device list state
  const [devices, setDevices] = useState<PaginatedResponse<DeviceResponse> | null>(null);
  const [devicesLoading, setDevicesLoading] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [editDescriptionDialogVisible, setEditDescriptionDialogVisible] = useState(false);
  const [deleteConfirmDialogVisible, setDeleteConfirmDialogVisible] = useState(false);
  const [selectedDevice, setSelectedDevice] = useState<DeviceResponse | null>(null);
  const [newDescription, setNewDescription] = useState('');

  // Auto-select building if there's only one
  useEffect(() => {
    if (lookupData && lookupData.buildings.length === 1) {
      setSelectedBuilding(lookupData.buildings[0]);
    }
  }, [lookupData]);

  // Auto-select room if there's only one
  useEffect(() => {
    if (selectedBuilding && selectedBuilding.rooms.length === 1) {
      setSelectedRoom(selectedBuilding.rooms[0]);
    }
  }, [selectedBuilding]);

  const loadDevices = async () => {
    setDevicesLoading(true);
    try {
      const data = await getDevices(currentPage, 10);
      setDevices(data);
      // Mark this page as loaded
      devicesLoadedForPage.current = currentPage;
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to load devices',
        life: 3000,
      });
    } finally {
      setDevicesLoading(false);
    }
  };

  // Load devices when page changes
  useEffect(() => {
    // Prevent double loading in React StrictMode
    if (devicesLoadedForPage.current === currentPage) {
      console.log(`Devices already loaded for page ${currentPage}, skipping...`);
      return;
    }
    loadDevices();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentPage]);

  // Initialize SignalR connection
  useEffect(() => {
    // Prevent double initialization in React StrictMode
    if (hubInitialized.current) {
      console.log('SignalR already initialized, skipping...');
      return;
    }

    // Set flag synchronously before any async operations
    hubInitialized.current = true;

    const initializeHub = async () => {
      try {
        console.log('Initializing SignalR hub...');
        // Start connection first to create the hub connection object
        await userHubService.start();

        // Then subscribe to device status notifications
        userHubService.onDeviceStatus((notification: DeviceStatusNotification) => {
          console.log('Received device status notification:', notification);

          // Show toast notification
          toastRef.current?.show({
            severity: notification.online ? 'success' : 'info',
            summary: notification.online ? 'Device Online' : 'Device Offline',
            detail: `${notification.deviceName} is now ${notification.online ? 'online' : 'offline'}${
              notification.roomName ? ` (${notification.buildingName} - ${notification.roomName})` : ''
            }`,
            life: 5000,
          });

          // Update the device list to reflect the new status
          setDevices((prevDevices) => {
            if (!prevDevices) return prevDevices;

            return {
              ...prevDevices,
              items: prevDevices.items.map((device) =>
                device.id === notification.deviceId
                  ? { ...device, online: notification.online }
                  : device
              ),
            };
          });
        });
      } catch (error) {
        console.error('Failed to initialize SignalR hub:', error);
        toastRef.current?.show({
          severity: 'warn',
          summary: 'Connection Issue',
          detail: 'Real-time updates may not be available',
          life: 5000,
        });
      }
    };

    initializeHub();

    // Cleanup: Do NOT reset hubInitialized flag to prevent re-initialization in StrictMode
    // The connection should persist for the lifetime of the app
  }, []);

  const handleEditDescription = (device: DeviceResponse) => {
    setSelectedDevice(device);
    setNewDescription(device.description || '');
    setEditDescriptionDialogVisible(true);
  };

  const handleSaveDescription = async () => {
    if (!selectedDevice) return;

    setIsLoading(true);
    try {
      await updateDeviceDescription({
        deviceId: selectedDevice.id,
        description: newDescription,
      });

      toastRef.current?.show({
        severity: 'success',
        summary: 'Success',
        detail: 'Device description updated successfully',
        life: 3000,
      });

      setEditDescriptionDialogVisible(false);
      devicesLoadedForPage.current = null; // Clear cache to force reload
      loadDevices(); // Reload devices
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to update description',
        life: 3000,
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteDevice = (device: DeviceResponse) => {
    setSelectedDevice(device);
    setDeleteConfirmDialogVisible(true);
  };

  const handleConfirmDelete = async () => {
    if (!selectedDevice) return;

    setIsLoading(true);
    try {
      await deleteDevice(selectedDevice.id);

      toastRef.current?.show({
        severity: 'success',
        summary: 'Success',
        detail: 'Device deleted successfully',
        life: 3000,
      });

      setDeleteConfirmDialogVisible(false);
      setSelectedDevice(null);
      devicesLoadedForPage.current = null; // Clear cache to force reload
      loadDevices(); // Reload devices
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to delete device',
        life: 3000,
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handlePingDevice = async (deviceId: string, deviceName: string) => {
    try {
      await userHubService.pingDevice(deviceId);
      toastRef.current?.show({
        severity: 'info',
        summary: 'Ping Sent',
        detail: `Ping command sent to ${deviceName}`,
        life: 3000,
      });
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to ping device',
        life: 3000,
      });
    }
  };

  const handleShutdownDevice = async (deviceId: string, deviceName: string) => {
    try {
      await userHubService.shutdownDevice(deviceId);
      toastRef.current?.show({
        severity: 'info',
        summary: 'Shutdown Command Sent',
        detail: `Shutdown command sent to ${deviceName}`,
        life: 3000,
      });
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to send shutdown command',
        life: 3000,
      });
    }
  };

  const handleRestartDevice = async (deviceId: string, deviceName: string) => {
    try {
      await userHubService.restartDevice(deviceId);
      toastRef.current?.show({
        severity: 'info',
        summary: 'Restart Command Sent',
        detail: `Restart command sent to ${deviceName}`,
        life: 3000,
      });
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to send restart command',
        life: 3000,
      });
    }
  };

  const handleRegisterDevice = () => {
    setRegisterDialogVisible(true);
    setDeviceCode('');
    setLookupData(null);
    setSelectedBuilding(null);
    setSelectedRoom(null);
  };

  const handleDownloadClient = () => {
    window.open('https://github.com/andyjmorgan/DonkeyWork-DeviceManager/releases', '_blank');
  };

  const handleLookupDeviceCode = async () => {
    if (!deviceCode.trim()) {
      toastRef.current?.show({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please enter a three word code',
        life: 3000,
      });
      return;
    }

    setIsLoading(true);
    try {
      const accessToken = localStorage.getItem('access_token');
      if (!accessToken) {
        throw new Error('Not authenticated');
      }

      const data = await lookupDeviceRegistration(deviceCode, accessToken);
      setLookupData(data);

      toastRef.current?.show({
        severity: 'success',
        summary: 'Device Found',
        detail: 'Please select a building and room for this device',
        life: 3000,
      });
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Lookup Failed',
        detail: error instanceof Error ? error.message : 'Failed to lookup device',
        life: 3000,
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleCompleteRegistration = async () => {
    if (!selectedRoom) {
      toastRef.current?.show({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please select a room',
        life: 3000,
      });
      return;
    }

    setIsLoading(true);
    try {
      const accessToken = localStorage.getItem('access_token');
      if (!accessToken) {
        throw new Error('Not authenticated');
      }

      const response = await completeDeviceRegistration(
        {
          threeWordCode: deviceCode,
          roomId: selectedRoom.id,
        },
        accessToken
      );

      toastRef.current?.show({
        severity: 'success',
        summary: 'Success',
        detail: response.message,
        life: 5000,
      });

      setDeviceCode('');
      setLookupData(null);
      setSelectedBuilding(null);
      setSelectedRoom(null);
      setRegisterDialogVisible(false);

      // Reload devices to show the newly registered device
      devicesLoadedForPage.current = null;
      loadDevices();
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Registration Failed',
        detail: error instanceof Error ? error.message : 'Failed to register device',
        life: 3000,
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleBuildingChange = (building: BuildingDto) => {
    setSelectedBuilding(building);
    setSelectedRoom(null);
  };

  return (
    <div className="dashboard-page">
      <Toast ref={toastRef} />
      <div className="dashboard-container">
        <h2>Welcome to DonkeyWork Device Manager</h2>

        <div className="action-cards">
          <Card title="Download Client" className="action-card">
            <p>Download the DonkeyWork client application to manage your devices</p>
            <Button
              label="Download"
              icon="pi pi-download"
              onClick={handleDownloadClient}
              className="action-button"
            />
          </Card>

          <Card title="Register Device" className="action-card">
            <p>Register a new device using your three word code</p>
            <Button
              label="Register"
              icon="pi pi-plus"
              onClick={handleRegisterDevice}
              className="action-button"
            />
          </Card>
        </div>

        <div className="devices-section">
          <h3>My Devices</h3>
          {devicesLoading ? (
            <div style={{ display: 'flex', justifyContent: 'center', padding: '2rem' }}>
              <ProgressSpinner />
            </div>
          ) : (
            <>
              <DataTable
                value={devices?.items || []}
                responsiveLayout="scroll"
                stripedRows
                emptyMessage="No devices found"
              >
                <Column
                  field="online"
                  header="Status"
                  body={(rowData: DeviceResponse) => (
                    <Tag
                      value={rowData.online ? 'Online' : 'Offline'}
                      severity={rowData.online ? 'success' : 'danger'}
                      icon={rowData.online ? 'pi pi-check' : 'pi pi-times'}
                    />
                  )}
                  sortable
                  style={{ width: '120px' }}
                />
                <Column field="name" header="Device Name" sortable />
                <Column
                  field="operatingSystem"
                  header="Device Type"
                  body={(rowData: DeviceResponse) => rowData.operatingSystem || '-'}
                  sortable
                  style={{ width: '150px' }}
                />
                <Column
                  field="room.name"
                  header="Location"
                  body={(rowData: DeviceResponse) => `${rowData.room.building.name} - ${rowData.room.name}`}
                  sortable
                />
                <Column
                  field="description"
                  header="Description"
                  body={(rowData: DeviceResponse) => rowData.description || '-'}
                />
                <Column
                  header="Actions"
                  body={(rowData: DeviceResponse) => {
                    const deviceMenuItems = [
                      {
                        label: 'Edit Description',
                        icon: 'pi pi-pencil',
                        command: () => handleEditDescription(rowData),
                      },
                      {
                        separator: true,
                      },
                      {
                        label: 'Ping',
                        icon: 'pi pi-wifi',
                        command: () => handlePingDevice(rowData.id, rowData.name),
                        disabled: !rowData.online,
                      },
                      {
                        label: 'Restart',
                        icon: 'pi pi-refresh',
                        command: () => handleRestartDevice(rowData.id, rowData.name),
                        disabled: !rowData.online,
                      },
                      {
                        label: 'Shutdown',
                        icon: 'pi pi-power-off',
                        command: () => handleShutdownDevice(rowData.id, rowData.name),
                        disabled: !rowData.online,
                      },
                      {
                        separator: true,
                      },
                      {
                        label: 'Delete',
                        icon: 'pi pi-trash',
                        command: () => handleDeleteDevice(rowData),
                        className: 'p-menuitem-danger',
                      },
                    ];

                    return (
                      <SplitButton
                        label="Manage"
                        icon="pi pi-cog"
                        model={deviceMenuItems}
                        onClick={() => handleEditDescription(rowData)}
                        className="p-button-sm"
                        size="small"
                      />
                    );
                  }}
                  style={{ width: '160px' }}
                />
              </DataTable>
              {devices && devices.totalPages > 1 && (
                <Paginator
                  first={(currentPage - 1) * 10}
                  rows={10}
                  totalRecords={devices.totalCount}
                  onPageChange={(e) => setCurrentPage(e.page + 1)}
                  style={{ marginTop: '1rem' }}
                />
              )}
            </>
          )}
        </div>
      </div>

      <Dialog
        header="Edit Device Description"
        visible={editDescriptionDialogVisible}
        style={{ width: '500px' }}
        onHide={() => setEditDescriptionDialogVisible(false)}
        footer={
          <div>
            <Button
              label="Cancel"
              icon="pi pi-times"
              onClick={() => setEditDescriptionDialogVisible(false)}
              className="p-button-text"
              disabled={isLoading}
            />
            <Button
              label="Save"
              icon="pi pi-check"
              onClick={handleSaveDescription}
              disabled={isLoading}
            />
          </div>
        }
      >
        <div className="register-dialog-content">
          <label htmlFor="device-description" className="register-label">
            Description
          </label>
          <InputText
            id="device-description"
            value={newDescription}
            onChange={(e) => setNewDescription(e.target.value)}
            placeholder="Enter device description"
            className="register-input"
          />
        </div>
      </Dialog>

      <Dialog
        header="Delete Device"
        visible={deleteConfirmDialogVisible}
        style={{ width: '450px' }}
        onHide={() => setDeleteConfirmDialogVisible(false)}
        footer={
          <div>
            <Button
              label="Cancel"
              icon="pi pi-times"
              onClick={() => setDeleteConfirmDialogVisible(false)}
              className="p-button-text"
              disabled={isLoading}
            />
            <Button
              label="Delete"
              icon="pi pi-trash"
              onClick={handleConfirmDelete}
              className="p-button-danger"
              disabled={isLoading}
            />
          </div>
        }
      >
        <div className="register-dialog-content">
          <p>
            <i className="pi pi-exclamation-triangle" style={{ fontSize: '2rem', color: 'var(--red-500)', marginRight: '1rem' }}></i>
            Are you sure you want to delete device <strong>{selectedDevice?.name}</strong>?
          </p>
          <p style={{ marginTop: '1rem', color: 'var(--text-color-secondary)' }}>
            This action cannot be undone. The device will be removed from both the database and Keycloak.
          </p>
        </div>
      </Dialog>

      <Dialog
        header="Register Device"
        visible={registerDialogVisible}
        style={{ width: '500px' }}
        onHide={() => setRegisterDialogVisible(false)}
        footer={
          !lookupData ? (
            <div>
              <Button
                label="Cancel"
                icon="pi pi-times"
                onClick={() => setRegisterDialogVisible(false)}
                className="p-button-text"
                disabled={isLoading}
              />
              <Button
                label="Lookup"
                icon="pi pi-search"
                onClick={handleLookupDeviceCode}
                disabled={isLoading}
              />
            </div>
          ) : (
            <div>
              <Button
                label="Back"
                icon="pi pi-arrow-left"
                onClick={() => {
                  setLookupData(null);
                  setSelectedBuilding(null);
                  setSelectedRoom(null);
                }}
                className="p-button-text"
                disabled={isLoading}
              />
              <Button
                label="Complete Registration"
                icon="pi pi-check"
                onClick={handleCompleteRegistration}
                disabled={isLoading || !selectedRoom}
              />
            </div>
          )
        }
      >
        {isLoading ? (
          <div className="register-loading">
            <ProgressSpinner />
          </div>
        ) : !lookupData ? (
          <div className="register-dialog-content">
            <label htmlFor="device-code" className="register-label">
              Three Word Code
            </label>
            <InputText
              id="device-code"
              value={deviceCode}
              onChange={(e) => setDeviceCode(e.target.value)}
              placeholder="enter-your-code"
              className="register-input"
            />
            <small className="register-help">
              Enter the three word code displayed on your device
            </small>
          </div>
        ) : (
          <div className="register-dialog-content">
            <div className="form-field">
              <label htmlFor="building" className="register-label">
                Building
              </label>
              <Dropdown
                id="building"
                value={selectedBuilding}
                options={lookupData.buildings}
                onChange={(e) => handleBuildingChange(e.value)}
                optionLabel="name"
                placeholder="Select a building"
                className="register-dropdown"
              />
            </div>

            {selectedBuilding && (
              <div className="form-field">
                <label htmlFor="room" className="register-label">
                  Room
                </label>
                <Dropdown
                  id="room"
                  value={selectedRoom}
                  options={selectedBuilding.rooms}
                  onChange={(e) => setSelectedRoom(e.value)}
                  optionLabel="name"
                  placeholder="Select a room"
                  className="register-dropdown"
                />
              </div>
            )}
          </div>
        )}
      </Dialog>
    </div>
  );
}

export default Dashboard;
