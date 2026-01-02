import { useState, useEffect, useRef } from 'react';
import { Button } from 'primereact/button';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Dialog } from 'primereact/dialog';
import { InputText } from 'primereact/inputtext';
import { InputTextarea } from 'primereact/inputtextarea';
import { Toast } from 'primereact/toast';
import { ProgressSpinner } from 'primereact/progressspinner';
import { ConfirmDialog, confirmDialog } from 'primereact/confirmdialog';
import { Building2, Plus, Pencil, Trash2 } from 'lucide-react';
import { getBuildings, createBuilding, updateBuilding, deleteBuilding } from '../services/buildingService';
import type { BuildingResponse, CreateBuildingRequest, UpdateBuildingRequest } from '../types/building';
import './Buildings.css';

function Buildings() {
  const toastRef = useRef<Toast>(null);
  const [buildings, setBuildings] = useState<BuildingResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [dialogVisible, setDialogVisible] = useState(false);
  const [editMode, setEditMode] = useState(false);
  const [selectedBuilding, setSelectedBuilding] = useState<BuildingResponse | null>(null);
  const [formData, setFormData] = useState<CreateBuildingRequest>({
    name: '',
    description: '',
  });
  const [saving, setSaving] = useState(false);
  const [first, setFirst] = useState(0);
  const [rows, setRows] = useState(10);

  useEffect(() => {
    loadBuildings();
  }, []);

  const loadBuildings = async () => {
    setLoading(true);
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
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditMode(false);
    setSelectedBuilding(null);
    setFormData({ name: '', description: '' });
    setDialogVisible(true);
  };

  const handleEdit = (building: BuildingResponse) => {
    setEditMode(true);
    setSelectedBuilding(building);
    setFormData({
      name: building.name,
      description: building.description || '',
    });
    setDialogVisible(true);
  };

  const handleSave = async () => {
    if (!formData.name.trim()) {
      toastRef.current?.show({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Building name is required',
        life: 3000,
      });
      return;
    }

    setSaving(true);
    try {
      if (editMode && selectedBuilding) {
        const request: UpdateBuildingRequest = {
          name: formData.name,
          description: formData.description || undefined,
        };
        await updateBuilding(selectedBuilding.id, request);
        toastRef.current?.show({
          severity: 'success',
          summary: 'Success',
          detail: 'Building updated successfully',
          life: 3000,
        });
      } else {
        const request: CreateBuildingRequest = {
          name: formData.name,
          description: formData.description || undefined,
        };
        await createBuilding(request);
        toastRef.current?.show({
          severity: 'success',
          summary: 'Success',
          detail: 'Building created successfully',
          life: 3000,
        });
      }
      setDialogVisible(false);
      loadBuildings();
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to save building',
        life: 3000,
      });
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = (building: BuildingResponse) => {
    confirmDialog({
      message: `Are you sure you want to delete "${building.name}"? This will also delete all rooms in this building.`,
      header: 'Delete Building',
      icon: 'pi pi-exclamation-triangle',
      acceptClassName: 'p-button-danger',
      accept: async () => {
        try {
          await deleteBuilding(building.id);
          toastRef.current?.show({
            severity: 'success',
            summary: 'Success',
            detail: 'Building deleted successfully',
            life: 3000,
          });
          loadBuildings();
        } catch (error) {
          toastRef.current?.show({
            severity: 'error',
            summary: 'Error',
            detail: error instanceof Error ? error.message : 'Failed to delete building',
            life: 3000,
          });
        }
      },
    });
  };

  const actionsTemplate = (rowData: BuildingResponse) => {
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

  const dateTemplate = (rowData: BuildingResponse, field: 'created' | 'updated') => {
    return new Date(rowData[field]).toLocaleDateString();
  };

  return (
    <div className="buildings-page">
      <Toast ref={toastRef} />
      <ConfirmDialog />

      <div className="page-header">
        <div className="page-title">
          <Building2 size={32} />
          <h1>Buildings</h1>
          {!loading && <span className="count-badge">{buildings.length} total</span>}
        </div>
        <Button
          label="Add Building"
          icon={<Plus size={18} />}
          onClick={handleCreate}
        />
      </div>

      <div className="page-content">
        {loading ? (
          <div className="loading-container">
            <ProgressSpinner />
          </div>
        ) : (
          <DataTable
            value={buildings}
            stripedRows
            emptyMessage="No buildings found"
            className="buildings-table"
            paginator
            rows={rows}
            first={first}
            onPage={(e) => {
              setFirst(e.first);
              setRows(e.rows);
            }}
            rowsPerPageOptions={[5, 10, 25, 50]}
            paginatorTemplate="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink CurrentPageReport RowsPerPageDropdown"
            currentPageReportTemplate="Showing {first} to {last} of {totalRecords} buildings"
          >
            <Column field="name" header="Name" sortable />
            <Column field="description" header="Description" body={(rowData) => rowData.description || '-'} />
            <Column
              field="roomCount"
              header="Rooms"
              body={(rowData) => rowData.roomCount || 0}
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
        header={editMode ? 'Edit Building' : 'Create Building'}
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
            <label htmlFor="name">Name *</label>
            <InputText
              id="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="Enter building name"
              className="w-full"
            />
          </div>

          <div className="form-field">
            <label htmlFor="description">Description</label>
            <InputTextarea
              id="description"
              value={formData.description || ''}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              placeholder="Enter building description"
              rows={4}
              className="w-full"
            />
          </div>
        </div>
      </Dialog>
    </div>
  );
}

export default Buildings;
