// Device Registration DTOs matching backend contracts

export interface DeviceRegistrationLookupDto {
  registrationId: string;
  threeWordCode: string;
  buildings: BuildingDto[];
}

export interface BuildingDto {
  id: string;
  name: string;
  rooms: RoomDto[];
}

export interface RoomDto {
  id: string;
  name: string;
}

export interface CompleteRegistrationRequest {
  threeWordCode: string;
  roomId: string;
}

export interface CompleteRegistrationResponse {
  success: boolean;
  message: string;
  deviceUserId: string;
}

export interface ApiError {
  error: string;
}
