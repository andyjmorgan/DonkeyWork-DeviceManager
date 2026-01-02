import { useRef } from 'react';
import { Button } from 'primereact/button';
import { Menu } from 'primereact/menu';
import type { MenuItem } from 'primereact/menuitem';
import type { DeviceResponse } from '../types/device';

interface DeviceActionsMenuProps {
  device: DeviceResponse;
  onEditDescription: (device: DeviceResponse) => void;
  onPing: (deviceId: string, deviceName: string) => void;
  onRunOSQuery: (device: DeviceResponse) => void;
  onRestart: (deviceId: string, deviceName: string) => void;
  onShutdown: (deviceId: string, deviceName: string) => void;
  onDelete: (device: DeviceResponse) => void;
}

function DeviceActionsMenu({
  device,
  onEditDescription,
  onPing,
  onRunOSQuery,
  onRestart,
  onShutdown,
  onDelete,
}: DeviceActionsMenuProps) {
  const menuRef = useRef<Menu>(null);

  const menuItems: MenuItem[] = [
    {
      label: 'Edit Description',
      icon: 'pi pi-pencil',
      command: () => onEditDescription(device),
    },
    {
      separator: true,
    },
    {
      label: 'Ping',
      icon: 'pi pi-wifi',
      command: () => onPing(device.id, device.name),
      disabled: !device.online,
    },
    {
      label: 'Run OSQuery',
      icon: 'pi pi-search',
      command: () => onRunOSQuery(device),
    },
    {
      label: 'Restart',
      icon: 'pi pi-refresh',
      command: () => onRestart(device.id, device.name),
      disabled: !device.online,
    },
    {
      label: 'Shutdown',
      icon: 'pi pi-power-off',
      command: () => onShutdown(device.id, device.name),
      disabled: !device.online,
    },
    {
      separator: true,
    },
    {
      label: 'Delete',
      icon: 'pi pi-trash',
      command: () => onDelete(device),
      className: 'p-menuitem-danger',
    },
  ];

  return (
    <>
      <Button
        icon="pi pi-ellipsis-v"
        rounded
        text
        onClick={(e) => menuRef.current?.toggle(e)}
        aria-label="Actions"
      />
      <Menu model={menuItems} popup ref={menuRef} />
    </>
  );
}

export default DeviceActionsMenu;
