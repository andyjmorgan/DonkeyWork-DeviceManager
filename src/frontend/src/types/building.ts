export interface BuildingResponse {
  id: string;
  name: string;
  description: string | null;
  created: string;
  updated: string;
  roomCount?: number;
}

export interface BuildingDetailsResponse {
  id: string;
  name: string;
  description: string | null;
  created: string;
  updated: string;
  rooms: Array<{
    id: string;
    name: string;
    description: string | null;
  }>;
}

export interface CreateBuildingRequest {
  name: string;
  description?: string;
}

export interface UpdateBuildingRequest {
  name: string;
  description?: string;
}
