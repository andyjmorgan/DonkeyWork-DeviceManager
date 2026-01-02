import { useState, useEffect, useRef } from 'react';
import { Button } from 'primereact/button';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Dialog } from 'primereact/dialog';
import { InputText } from 'primereact/inputtext';
import { InputTextarea } from 'primereact/inputtextarea';
import { Dropdown } from 'primereact/dropdown';
import { Toast } from 'primereact/toast';
import { ProgressSpinner } from 'primereact/progressspinner';
import { ConfirmDialog, confirmDialog } from 'primereact/confirmdialog';
import { DoorOpen, Plus, Pencil, Trash2 } from 'lucide-react';
import { getRooms, createRoom, updateRoom, deleteRoom } from '../services/roomService';
import { getBuildings } from '../services/buildingService';
import type { RoomResponse, CreateRoomRequest, UpdateRoomRequest } from '../types/room';
import type { BuildingResponse } from '../types/building';
import './Rooms.css';

function Rooms() {
  const toastRef = useRef<Toast>(null);
  const [rooms, setRooms] = useState<RoomResponse[]>([]);
  const [buildings, setBuildings] = useState<BuildingResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [dialogVisible, setDialogVisible] = useState(false);
  const [editMode, setEditMode] = useState(false);
  const [selectedRoom, setSelectedRoom] = useState<RoomResponse | null>(null);
  const [formData, setFormData] = useState<CreateRoomRequest>({
    name: '',
    description: '',
    buildingId: '',
  });
  const [saving, setSaving] = useState(false);
  const [filterBuildingId, setFilterBuildingId] = useState<string | null>(null);
  const [first, setFirst] = useState(0);
  const [rows, setRows] = useState(10);

  useEffect(() => {
    loadBuildings();
    loadRooms();
  }, []);

  useEffect(() => {
    loadRooms(filterBuildingId || undefined);
  }, [filterBuildingId]);

  const loadBuildings = async () => {
    try {
      const data = await getBuildings();
      setBuildings(data);
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to load buildings',
        life: 3000,
      });
    }
  };

  const loadRooms = async (buildingId?: string) => {
    setLoading(true);
    try {
      const data = await getRooms(buildingId);
      setRooms(data);
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to load rooms',
        life: 3000,
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditMode(false);
    setSelectedRoom(null);
    setFormData({ name: '', description: '', buildingId: '' });
    setDialogVisible(true);
  };

  const handleEdit = (room: RoomResponse) => {
    setEditMode(true);
    setSelectedRoom(room);
    setFormData({
      name: room.name,
      description: room.description || '',
      buildingId: room.buildingId,
    });
    setDialogVisible(true);
  };

  const handleSave = async () => {
    if (!formData.name.trim()) {
      toastRef.current?.show({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Room name is required',
        life: 3000,
      });
      return;
    }

    if (!formData.buildingId) {
      toastRef.current?.show({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Building is required',
        life: 3000,
      });
      return;
    }

    setSaving(true);
    try {
      if (editMode && selectedRoom) {
        const request: UpdateRoomRequest = {
          name: formData.name,
          description: formData.description || undefined,
          buildingId: formData.buildingId,
        };
        await updateRoom(selectedRoom.id, request);
        toastRef.current?.show({
          severity: 'success',
          summary: 'Success',
          detail: 'Room updated successfully',
          life: 3000,
        });
      } else {
        const request: CreateRoomRequest = {
          name: formData.name,
          description: formData.description || undefined,
          buildingId: formData.buildingId,
        };
        await createRoom(request);
        toastRef.current?.show({
          severity: 'success',
          summary: 'Success',
          detail: 'Room created successfully',
          life: 3000,
        });
      }
      setDialogVisible(false);
      loadRooms(filterBuildingId || undefined);
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to save room',
        life: 3000,
      });
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = (room: RoomResponse) => {
    confirmDialog({
      message: `Are you sure you want to delete "${room.name}"? This will affect all devices in this room.`,
      header: 'Delete Room',
      icon: 'pi pi-exclamation-triangle',
      acceptClassName: 'p-button-danger',
      accept: async () => {
        try {
          await deleteRoom(room.id);
          toastRef.current?.show({
            severity: 'success',
            summary: 'Success',
            detail: 'Room deleted successfully',
            life: 3000,
          });
          loadRooms(filterBuildingId || undefined);
        } catch (error) {
          toastRef.current?.show({
            severity: 'error',
            summary: 'Error',
            detail: error instanceof Error ? error.message : 'Failed to delete room',
            life: 3000,
          });
        }
      },
    });
  };

  const actionsTemplate = (rowData: RoomResponse) => {
    return (
      <div className="action-buttons">
        <Button
          icon={<Pencil size={16} />}
          rounded
          text
          severity="info"
          onClick={() => handleEdit(rowData)}
          tooltip="Edit"
          tooltipOptions={{ position: 'top' }}
        />
        <Button
          icon={<Trash2 size={16} />}
          rounded
          text
          severity="danger"
          onClick={() => handleDelete(rowData)}
          tooltip="Delete"
          tooltipOptions={{ position: 'top' }}
        />
      </div>
    );
  };

  const dateTemplate = (rowData: RoomResponse, field: 'created' | 'updated') => {
    return new Date(rowData[field]).toLocaleDateString();
  };

  return (
    <div className="rooms-page">
      <Toast ref={toastRef} />
      <ConfirmDialog />

      <div className="page-header">
        <div className="page-title">
          <DoorOpen size={32} />
          <h1>Rooms</h1>
          {!loading && <span className="count-badge">{rooms.length} total</span>}
        </div>
        <div className="header-actions">
          <Dropdown
            value={filterBuildingId}
            options={[
              { label: 'All Buildings', value: null },
              ...buildings.map((b) => ({ label: b.name, value: b.id })),
            ]}
            onChange={(e) => setFilterBuildingId(e.value)}
            placeholder="Filter by building"
            className="filter-dropdown"
          />
          <Button
            label="Add Room"
            icon={<Plus size={18} />}
            onClick={handleCreate}
          />
        </div>
      </div>

      <div className="page-content">
        {loading ? (
          <div className="loading-container">
            <ProgressSpinner />
          </div>
        ) : (
          <DataTable
            value={rooms}
            stripedRows
            emptyMessage="No rooms found"
            className="rooms-table"
            paginator
            rows={rows}
            first={first}
            onPage={(e) => {
              setFirst(e.first);
              setRows(e.rows);
            }}
            rowsPerPageOptions={[5, 10, 25, 50]}
            paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
            currentPageReportTemplate="Showing {first} to {last} of {totalRecords} rooms"
          >
            <Column field="name" header="Name" sortable />
            <Column field="buildingName" header="Building" sortable />
            <Column field="description" header="Description" body={(rowData) => rowData.description || '-'} />
            <Column
              field="deviceCount"
              header="Devices"
              body={(rowData) => rowData.deviceCount || 0}
              style={{ width: '100px' }}
            />
            <Column
              field="created"
              header="Created"
              body={(rowData) => dateTemplate(rowData, 'created')}
              sortable
              style={{ width: '120px' }}
            />
            <Column
              header="Actions"
              body={actionsTemplate}
              style={{ width: '120px' }}
            />
          </DataTable>
        )}
      </div>

      <Dialog
        header={editMode ? 'Edit Room' : 'Create Room'}
        visible={dialogVisible}
        style={{ width: '500px' }}
        onHide={() => setDialogVisible(false)}
        footer={
          <div>
            <Button
              label="Cancel"
              icon="pi pi-times"
              onClick={() => setDialogVisible(false)}
              className="p-button-text"
              disabled={saving}
            />
            <Button
              label="Save"
              icon="pi pi-check"
              onClick={handleSave}
              disabled={saving}
            />
          </div>
        }
      >
        <div className="form-content">
          <div className="form-field">
            <label htmlFor="building">Building *</label>
            <Dropdown
              id="building"
              value={formData.buildingId}
              options={buildings.map((b) => ({ label: b.name, value: b.id }))}
              onChange={(e) => setFormData({ ...formData, buildingId: e.value })}
              placeholder="Select a building"
              className="w-full"
            />
          </div>

          <div className="form-field">
            <label htmlFor="name">Name *</label>
            <InputText
              id="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="Enter room name"
              className="w-full"
            />
          </div>

          <div className="form-field">
            <label htmlFor="description">Description</label>
            <InputTextarea
              id="description"
              value={formData.description || ''}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              placeholder="Enter room description"
              rows={4}
              className="w-full"
            />
          </div>
        </div>
      </Dialog>
    </div>
  );
}

export default Rooms;
