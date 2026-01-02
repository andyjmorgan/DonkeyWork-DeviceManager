export interface RoomResponse {
  id: string;
  name: string;
  description: string | null;
  buildingId: string;
  buildingName: string;
  created: string;
  updated: string;
  deviceCount?: number;
}

export interface RoomDetailsResponse {
  id: string;
  name: string;
  description: string | null;
  buildingId: string;
  buildingName: string;
  created: string;
  updated: string;
  devices: Array<{
    id: string;
    name: string;
    online: boolean;
  }>;
}

export interface CreateRoomRequest {
  name: string;
  description?: string;
  buildingId: string;
}

export interface UpdateRoomRequest {
  name: string;
  description?: string;
  buildingId?: string;
}
