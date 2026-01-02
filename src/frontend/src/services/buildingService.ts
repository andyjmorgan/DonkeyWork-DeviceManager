import { authenticatedFetch } from '../utils/apiClient';
import type {
  BuildingResponse,
  BuildingDetailsResponse,
  CreateBuildingRequest,
  UpdateBuildingRequest,
} from '../types/building';

/**
 * Get all buildings for the current tenant
 */
export const getBuildings = async (): Promise<BuildingResponse[]> => {
  const response = await authenticatedFetch('/api/buildings', {
    method: 'GET',
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to fetch buildings' }));
    throw new Error(error.error || `Failed to fetch buildings: ${response.status}`);
  }

  return response.json();
};

/**
 * Get a building by ID with its rooms
 */
export const getBuildingById = async (id: string): Promise<BuildingDetailsResponse> => {
  const response = await authenticatedFetch(`/api/buildings/${id}`, {
    method: 'GET',
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to fetch building' }));
    throw new Error(error.error || `Failed to fetch building: ${response.status}`);
  }

  return response.json();
};

/**
 * Create a new building
 */
export const createBuilding = async (request: CreateBuildingRequest): Promise<BuildingResponse> => {
  const response = await authenticatedFetch('/api/buildings', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to create building' }));
    throw new Error(error.error || `Failed to create building: ${response.status}`);
  }

  return response.json();
};

/**
 * Update a building
 */
export const updateBuilding = async (
  id: string,
  request: UpdateBuildingRequest
): Promise<BuildingResponse> => {
  const response = await authenticatedFetch(`/api/buildings/${id}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to update building' }));
    throw new Error(error.error || `Failed to update building: ${response.status}`);
  }

  return response.json();
};

/**
 * Delete a building
 */
export const deleteBuilding = async (id: string): Promise<void> => {
  const response = await authenticatedFetch(`/api/buildings/${id}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to delete building' }));
    throw new Error(error.error || `Failed to delete building: ${response.status}`);
  }
};
