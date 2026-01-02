import type {
  OSQueryHistoryResponse,
  OSQueryExecutionResponse,
  SaveQueryRequest,
  PaginatedResponse,
} from '../types/osquery';
import { authenticatedFetch } from '../utils/apiClient';

/**
 * Get paginated query history for the current user
 */
export const getQueryHistory = async (
  page: number = 1,
  pageSize: number = 20
): Promise<PaginatedResponse<OSQueryHistoryResponse>> => {
  const response = await authenticatedFetch(
    `/api/osquery/history?page=${page}&pageSize=${pageSize}`
  );

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to fetch query history' }));
    throw new Error(error.error || `Failed to fetch query history: ${response.status}`);
  }

  return response.json();
};

/**
 * Get a specific query from history by ID
 */
export const getQueryHistoryById = async (queryId: string): Promise<OSQueryHistoryResponse> => {
  const response = await authenticatedFetch(`/api/osquery/history/${queryId}`);

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to fetch query' }));
    throw new Error(error.error || `Failed to fetch query: ${response.status}`);
  }

  return response.json();
};

/**
 * Save a query to history
 */
export const saveQueryToHistory = async (
  request: SaveQueryRequest
): Promise<OSQueryHistoryResponse> => {
  const response = await authenticatedFetch('/api/osquery/history', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to save query' }));
    throw new Error(error.error || `Failed to save query: ${response.status}`);
  }

  return response.json();
};

/**
 * Delete a query from history
 */
export const deleteQueryFromHistory = async (queryId: string): Promise<void> => {
  const response = await authenticatedFetch(`/api/osquery/history/${queryId}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to delete query' }));
    throw new Error(error.error || `Failed to delete query: ${response.status}`);
  }
};

/**
 * Get a specific execution with all results
 */
export const getExecution = async (executionId: string): Promise<OSQueryExecutionResponse> => {
  const response = await authenticatedFetch(`/api/osquery/executions/${executionId}`);

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to fetch execution' }));
    throw new Error(error.error || `Failed to fetch execution: ${response.status}`);
  }

  return response.json();
};

/**
 * Get recent executions for the current user
 */
export const getRecentExecutions = async (
  limit: number = 10
): Promise<OSQueryExecutionResponse[]> => {
  const response = await authenticatedFetch(`/api/osquery/executions?limit=${limit}`);

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to fetch recent executions' }));
    throw new Error(error.error || `Failed to fetch recent executions: ${response.status}`);
  }

  return response.json();
};
