import { useState, useEffect, useCallback, useRef } from 'react';
import { Button } from 'primereact/button';
import { MultiSelect } from 'primereact/multiselect';
import { ProgressSpinner } from 'primereact/progressspinner';
import { Toast } from 'primereact/toast';
import { Message } from 'primereact/message';
import { Sidebar } from 'primereact/sidebar';
import { MonacoQueryEditor } from '../components/MonacoQueryEditor';
import { OSQueryDeviceResult } from '../components/OSQueryDeviceResult';
import { userHubService } from '../services/userHubService';
import { getDevices } from '../services/deviceService';
import {
  getQueryHistory,
  saveQueryToHistory,
  deleteQueryFromHistory
} from '../services/osqueryService';
import type { DeviceResponse } from '../types/device';
import type {
  OSQueryResult,
  OSQueryExecutionResultResponse,
  OSQueryHistoryResponse
} from '../types/osquery';
import './OSQuery.css';

const MAX_DEVICES = 20;

export default function OSQuery() {
  const toastRef = useRef<Toast>(null);
  const [query, setQuery] = useState('SELECT * FROM processes LIMIT 10;');
  const [selectedDevices, setSelectedDevices] = useState<DeviceResponse[]>([]);
  const [availableDevices, setAvailableDevices] = useState<DeviceResponse[]>([]);
  const [devicesLoading, setDevicesLoading] = useState(false);
  const [executing, setExecuting] = useState(false);
  const [executionIds, setExecutionIds] = useState<Map<string, string>>(new Map());
  const [results, setResults] = useState<OSQueryExecutionResultResponse[]>([]);
  const [historyVisible, setHistoryVisible] = useState(false);
  const [queryHistory, setQueryHistory] = useState<OSQueryHistoryResponse[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

  // Load available devices
  useEffect(() => {
    loadDevices();
  }, []);

  // Subscribe to OSQuery results
  useEffect(() => {
    const handleResult = (osqueryResult: OSQueryResult) => {
      // Find which device this result is for
      const deviceId = osqueryResult.deviceId;
      const executionId = osqueryResult.executionId;

      // Check if this result is for one of our current executions
      if (executionIds.has(deviceId) && executionIds.get(deviceId) === executionId) {
        const device = selectedDevices.find(d => d.id === deviceId);

        setResults(prev => [...prev, {
          id: crypto.randomUUID(),
          executionId: osqueryResult.executionId,
          deviceId: osqueryResult.deviceId,
          deviceName: device?.name || 'Unknown Device',
          success: osqueryResult.success,
          errorMessage: osqueryResult.errorMessage,
          rawJson: osqueryResult.rawJson,
          executionTimeMs: osqueryResult.executionTimeMs,
          rowCount: osqueryResult.rowCount,
          createdAt: new Date().toISOString(),
        }]);

        // Remove this execution from the map
        setExecutionIds(prev => {
          const newMap = new Map(prev);
          newMap.delete(deviceId);
          return newMap;
        });

        // Check if all executions are complete
        setExecutionIds(prev => {
          if (prev.size === 0) {
            setExecuting(false);
          }
          return prev;
        });
      }
    };

    try {
      userHubService.onOSQueryResult(handleResult);
    } catch (error) {
      console.error('Failed to subscribe to OSQuery results:', error);
    }

    return () => {
      try {
        userHubService.offOSQueryResult(handleResult);
      } catch (error) {
        console.error('Failed to unsubscribe from OSQuery results:', error);
      }
    };
  }, [executionIds, selectedDevices]);

  const loadDevices = async () => {
    setDevicesLoading(true);
    try {
      // Load all devices (paginate if needed)
      const response = await getDevices(1, 100);
      setAvailableDevices(response.items);
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to load devices',
        life: 3000,
      });
    } finally {
      setDevicesLoading(false);
    }
  };

  const loadQueryHistory = async () => {
    setHistoryLoading(true);
    try {
      const response = await getQueryHistory(1, 50);
      setQueryHistory(response.items);
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to load query history',
        life: 3000,
      });
    } finally {
      setHistoryLoading(false);
    }
  };

  const handleExecute = useCallback(async () => {
    if (!query.trim() || selectedDevices.length === 0) {
      toastRef.current?.show({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please enter a query and select at least one device',
        life: 3000,
      });
      return;
    }

    try {
      setExecuting(true);
      setResults([]);

      // Generate execution IDs for each device
      const newExecutionIds = new Map<string, string>();
      for (const device of selectedDevices) {
        const executionId = crypto.randomUUID();
        newExecutionIds.set(device.id, executionId);
      }
      setExecutionIds(newExecutionIds);

      // Send query to each device
      for (const device of selectedDevices) {
        const executionId = newExecutionIds.get(device.id)!;
        await userHubService.executeOSQuery(device.id, query, executionId);
      }

      // Save query to history (async, don't wait)
      saveQueryToHistory({ query }).catch((error) => {
        console.error('Failed to save query to history:', error);
      });

      toastRef.current?.show({
        severity: 'info',
        summary: 'Executing Query',
        detail: `Query sent to ${selectedDevices.length} device(s)`,
        life: 3000,
      });
    } catch (error) {
      console.error('Failed to execute OSQuery:', error);
      setExecuting(false);
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to execute query',
        life: 3000,
      });
    }
  }, [query, selectedDevices]);

  const handleClear = () => {
    setQuery('');
    setResults([]);
    setExecuting(false);
    setExecutionIds(new Map());
  };

  const handleSaveQuery = async () => {
    if (!query.trim()) {
      toastRef.current?.show({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please enter a query to save',
        life: 3000,
      });
      return;
    }

    try {
      await saveQueryToHistory({ query });
      toastRef.current?.show({
        severity: 'success',
        summary: 'Success',
        detail: 'Query saved to history',
        life: 3000,
      });
      loadQueryHistory(); // Refresh history
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to save query',
        life: 3000,
      });
    }
  };

  const handleLoadQuery = (historyItem: OSQueryHistoryResponse) => {
    setQuery(historyItem.query);
    setHistoryVisible(false);
  };

  const handleDeleteHistoryItem = async (id: string) => {
    try {
      await deleteQueryFromHistory(id);
      toastRef.current?.show({
        severity: 'success',
        summary: 'Success',
        detail: 'Query deleted from history',
        life: 3000,
      });
      loadQueryHistory(); // Refresh history
    } catch (error) {
      toastRef.current?.show({
        severity: 'error',
        summary: 'Error',
        detail: error instanceof Error ? error.message : 'Failed to delete query',
        life: 3000,
      });
    }
  };

  const handleShowHistory = () => {
    setHistoryVisible(true);
    loadQueryHistory();
  };

  const handleDownloadAllResults = () => {
    if (results.length === 0) return;

    const combinedResults = results.map(result => ({
      deviceId: result.deviceId,
      deviceName: result.deviceName,
      success: result.success,
      errorMessage: result.errorMessage,
      data: result.rawJson ? JSON.parse(result.rawJson) : null,
      executionTimeMs: result.executionTimeMs,
      rowCount: result.rowCount,
      timestamp: result.createdAt,
    }));

    const blob = new Blob([JSON.stringify(combinedResults, null, 2)], {
      type: 'application/json'
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `osquery-results-${new Date().toISOString()}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const onlineDevices = availableDevices.filter(d => d.online);
  const deviceWarning = selectedDevices.length > MAX_DEVICES;
  const hasOfflineDevices = selectedDevices.some(d => !d.online);
  const pendingCount = executionIds.size;
  const completedCount = results.length;

  return (
    <div className="osquery-page">
      <Toast ref={toastRef} />

      <div className="osquery-header">
        <h2>OSQuery Console</h2>
        <div className="osquery-header-actions">
          <Button
            label="History"
            icon="pi pi-history"
            onClick={handleShowHistory}
            outlined
          />
          <Button
            label="Save Query"
            icon="pi pi-save"
            onClick={handleSaveQuery}
            disabled={!query.trim()}
          />
        </div>
      </div>

      <div className="osquery-content">
        <div className="osquery-main">
          <div className="device-selector-section">
            <label className="section-label">Select Devices</label>
            <MultiSelect
              value={selectedDevices}
              options={onlineDevices}
              onChange={(e) => setSelectedDevices(e.value)}
              optionLabel="name"
              placeholder="Select devices to query"
              filter
              maxSelectedLabels={3}
              className="device-multiselect"
              disabled={devicesLoading || executing}
              display="chip"
            />
            <div className="device-selector-info">
              <span className="device-count">
                {selectedDevices.length} / {MAX_DEVICES} devices selected
              </span>
            </div>
          </div>

          {deviceWarning && (
            <Message
              severity="warn"
              text={`Warning: You have selected ${selectedDevices.length} devices. For best performance, consider limiting to ${MAX_DEVICES} devices or fewer.`}
              className="device-warning"
            />
          )}

          {hasOfflineDevices && (
            <Message
              severity="warn"
              text="Warning: Some selected devices are offline and will not receive the query."
              className="device-warning"
            />
          )}

          <div className="query-editor-section">
            <label className="section-label">SQL Query</label>
            <MonacoQueryEditor
              value={query}
              onChange={setQuery}
              height="300px"
              readOnly={executing}
            />
          </div>

          <div className="query-actions">
            <Button
              label="Clear"
              icon="pi pi-times"
              onClick={handleClear}
              severity="secondary"
              outlined
              disabled={executing}
            />
            <Button
              label={`Execute Query${selectedDevices.length > 0 ? ` (${selectedDevices.length} devices)` : ''}`}
              icon="pi pi-play"
              onClick={handleExecute}
              disabled={executing || !query.trim() || selectedDevices.length === 0}
              loading={executing}
            />
          </div>

          {executing && (
            <div className="execution-progress">
              <ProgressSpinner style={{ width: '30px', height: '30px' }} />
              <span className="progress-text">
                Executing query: {completedCount} completed, {pendingCount} pending...
              </span>
            </div>
          )}

          {results.length > 0 && (
            <div className="results-section">
              <div className="results-header">
                <h3>Results ({results.length} devices)</h3>
                <Button
                  label="Download All"
                  icon="pi pi-download"
                  size="small"
                  onClick={handleDownloadAllResults}
                  outlined
                />
              </div>
              <div className="results-list">
                {results.map((result) => (
                  <OSQueryDeviceResult
                    key={result.id}
                    result={result}
                    deviceName={result.deviceName}
                  />
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      <Sidebar
        visible={historyVisible}
        position="right"
        onHide={() => setHistoryVisible(false)}
        className="osquery-history-sidebar"
        style={{ width: '400px' }}
      >
        <h3>Query History</h3>
        {historyLoading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: '2rem' }}>
            <ProgressSpinner />
          </div>
        ) : queryHistory.length > 0 ? (
          <div className="history-list">
            {queryHistory.map((item) => (
              <div key={item.id} className="history-item">
                <div className="history-item-content" onClick={() => handleLoadQuery(item)}>
                  <div className="history-query">{item.query}</div>
                  <div className="history-meta">
                    <span>Executed {item.executionCount} times</span>
                    {item.lastExecutedAt && (
                      <span> · Last run: {new Date(item.lastExecutedAt).toLocaleDateString()}</span>
                    )}
                  </div>
                </div>
                <Button
                  icon="pi pi-trash"
                  text
                  severity="danger"
                  size="small"
                  onClick={(e) => {
                    e.stopPropagation();
                    handleDeleteHistoryItem(item.id);
                  }}
                  tooltip="Delete"
                  tooltipOptions={{ position: 'left' }}
                />
              </div>
            ))}
          </div>
        ) : (
          <p className="empty-history">No query history found</p>
        )}
      </Sidebar>
    </div>
  );
}
