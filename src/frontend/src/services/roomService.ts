import { authenticatedFetch } from '../utils/apiClient';
import type {
  RoomResponse,
  RoomDetailsResponse,
  CreateRoomRequest,
  UpdateRoomRequest,
} from '../types/room';

/**
 * Get all rooms for the current tenant, optionally filtered by building
 */
export const getRooms = async (buildingId?: string): Promise<RoomResponse[]> => {
  const url = buildingId
    ? `/api/rooms?buildingId=${encodeURIComponent(buildingId)}`
    : '/api/rooms';

  const response = await authenticatedFetch(url, {
    method: 'GET',
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to fetch rooms' }));
    throw new Error(error.error || `Failed to fetch rooms: ${response.status}`);
  }

  return response.json();
};

/**
 * Get a room by ID with its building and devices
 */
export const getRoomById = async (id: string): Promise<RoomDetailsResponse> => {
  const response = await authenticatedFetch(`/api/rooms/${id}`, {
    method: 'GET',
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to fetch room' }));
    throw new Error(error.error || `Failed to fetch room: ${response.status}`);
  }

  return response.json();
};

/**
 * Create a new room
 */
export const createRoom = async (request: CreateRoomRequest): Promise<RoomResponse> => {
  const response = await authenticatedFetch('/api/rooms', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to create room' }));
    throw new Error(error.error || `Failed to create room: ${response.status}`);
  }

  return response.json();
};

/**
 * Update a room
 */
export const updateRoom = async (
  id: string,
  request: UpdateRoomRequest
): Promise<RoomResponse> => {
  const response = await authenticatedFetch(`/api/rooms/${id}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to update room' }));
    throw new Error(error.error || `Failed to update room: ${response.status}`);
  }

  return response.json();
};

/**
 * Delete a room
 */
export const deleteRoom = async (id: string): Promise<void> => {
  const response = await authenticatedFetch(`/api/rooms/${id}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to delete room' }));
    throw new Error(error.error || `Failed to delete room: ${response.status}`);
  }
};
