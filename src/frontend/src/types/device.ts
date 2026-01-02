// Device DTOs matching backend contracts

export interface DeviceResponse {
  id: string;
  name: string;
  description: string | null;
  online: boolean;
  createdAt: string;
  lastSeen: string;
  room: DeviceRoomResponse;
  cpuCores: number | null;
  totalMemoryBytes: number | null;
  operatingSystem: string | null;
  osArchitecture: string | null;
  architecture: string | null;
  operatingSystemVersion: string | null;
}

export interface DeviceRoomResponse {
  id: string;
  name: string;
  building: DeviceBuildingResponse;
}

export interface DeviceBuildingResponse {
  id: string;
  name: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface UpdateDeviceDescriptionRequest {
  deviceId: string;
  description: string;
}
