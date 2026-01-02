import type {
  DeviceRegistrationLookupDto,
  CompleteRegistrationRequest,
  CompleteRegistrationResponse,
} from '../types/deviceRegistration';
import { authenticatedFetch } from '../utils/apiClient';

/**
 * Lookup device registration by three-word code
 */
export const lookupDeviceRegistration = async (
  code: string,
  _accessToken: string
): Promise<DeviceRegistrationLookupDto> => {
  const response = await authenticatedFetch(`/api/device-registration/lookup?code=${encodeURIComponent(code)}`, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Lookup failed' }));
    throw new Error(error.error || `Failed to lookup device: ${response.status}`);
  }

  return response.json();
};

/**
 * Complete device registration
 */
export const completeDeviceRegistration = async (
  request: CompleteRegistrationRequest,
  _accessToken: string
): Promise<CompleteRegistrationResponse> => {
  const response = await authenticatedFetch('/api/device-registration/complete', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Registration failed' }));
    throw new Error(error.error || `Failed to register device: ${response.status}`);
  }

  return response.json();
};
