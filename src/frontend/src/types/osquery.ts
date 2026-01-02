export interface OSQueryHistoryResponse {
  id: string;
  query: string;
  executionCount: number;
  lastExecutedAt: string | null;
  created: string;
  updated: string;
}

export interface OSQueryExecutionResultResponse {
  id: string;
  executionId: string;
  deviceId: string;
  deviceName: string;
  success: boolean;
  errorMessage: string | null;
  rawJson: string | null;
  executionTimeMs: number;
  rowCount: number;
  createdAt: string;
}

export interface OSQueryExecutionResponse {
  id: string;
  queryHistoryId: string | null;
  query: string;
  executedAt: string;
  deviceCount: number;
  successCount: number;
  failureCount: number;
  userId: string;
  created: string;
  results: OSQueryExecutionResultResponse[];
}

export interface SaveQueryRequest {
  query: string;
}

export interface ExecuteOSQueryRequest {
  query: string;
  deviceIds: string[];
  queryHistoryId?: string;
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

// Real-time result from SignalR
export interface OSQueryResult {
  deviceId: string;
  executionId: string;
  success: boolean;
  errorMessage: string | null;
  rawJson: string | null;
  executionTimeMs: number;
  rowCount: number;
  timestamp: string;
}

// Client-side state for tracking query execution
export interface OSQueryExecution {
  executionId: string;
  query: string;
  deviceIds: string[];
  startedAt: string;
  results: Map<string, OSQueryExecutionResultResponse>;
  completedCount: number;
  totalCount: number;
}
