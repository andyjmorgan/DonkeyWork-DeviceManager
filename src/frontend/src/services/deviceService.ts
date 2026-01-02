import type { DeviceResponse, PaginatedResponse, UpdateDeviceDescriptionRequest } from '../types/device';
import { authenticatedFetch } from '../utils/apiClient';

/**
 * Get paginated list of devices
 */
export const getDevices = async (
  page: number = 1,
  pageSize: number = 10
): Promise<PaginatedResponse<DeviceResponse>> => {
  const response = await authenticatedFetch(
    `/api/devices?page=${page}&pageSize=${pageSize}`
  );

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to fetch devices' }));
    throw new Error(error.error || `Failed to fetch devices: ${response.status}`);
  }

  const data = await response.json();
  console.log('API response:', data);

  // Handle both array response and paginated response
  if (Array.isArray(data)) {
    // Backend returned array instead of paginated response
    console.warn('Backend returned array instead of PaginatedResponse, wrapping it');
    return {
      items: data,
      page: page,
      pageSize: pageSize,
      totalCount: data.length,
      totalPages: Math.ceil(data.length / pageSize),
      hasPreviousPage: page > 1,
      hasNextPage: false,
    };
  }

  return data;
};

/**
 * Update device description
 */
export const updateDeviceDescription = async (
  request: UpdateDeviceDescriptionRequest
): Promise<void> => {
  const response = await authenticatedFetch(`/api/devices/${request.deviceId}/description`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ description: request.description }),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to update description' }));
    throw new Error(error.error || `Failed to update description: ${response.status}`);
  }
};

/**
 * Delete device
 */
export const deleteDevice = async (deviceId: string): Promise<void> => {
  const response = await authenticatedFetch(`/api/devices/${deviceId}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to delete device' }));
    throw new Error(error.error || `Failed to delete device: ${response.status}`);
  }
};
