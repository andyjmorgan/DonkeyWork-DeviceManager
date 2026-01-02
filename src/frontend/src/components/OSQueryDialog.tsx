import { useState, useEffect, useCallback } from 'react';
import { Dialog } from 'primereact/dialog';
import { Button } from 'primereact/button';
import { ProgressSpinner } from 'primereact/progressspinner';
import { MonacoQueryEditor } from './MonacoQueryEditor';
import { OSQueryDeviceResult } from './OSQueryDeviceResult';
import { userHubService } from '../services/userHubService';
import { saveQueryToHistory } from '../services/osqueryService';
import type { OSQueryResult, OSQueryExecutionResultResponse } from '../types/osquery';
import type { DeviceResponse } from '../types/device';

interface OSQueryDialogProps {
  visible: boolean;
  device: DeviceResponse | null;
  onHide: () => void;
}

export const OSQueryDialog: React.FC<OSQueryDialogProps> = ({
  visible,
  device,
  onHide,
}) => {
  const [query, setQuery] = useState('SELECT * FROM processes LIMIT 10;');
  const [executing, setExecuting] = useState(false);
  const [executionId, setExecutionId] = useState<string | null>(null);
  const [result, setResult] = useState<OSQueryExecutionResultResponse | null>(null);

  // Subscribe to OSQuery results
  useEffect(() => {
    if (!visible) return;

    const handleResult = (osqueryResult: OSQueryResult) => {
      // Only handle results for our current execution
      if (executionId && osqueryResult.executionId === executionId) {
        setResult({
          id: crypto.randomUUID(),
          executionId: osqueryResult.executionId,
          deviceId: osqueryResult.deviceId,
          deviceName: device?.name || 'Unknown Device',
          success: osqueryResult.success,
          errorMessage: osqueryResult.errorMessage,
          rawJson: osqueryResult.rawJson,
          executionTimeMs: osqueryResult.executionTimeMs,
          rowCount: osqueryResult.rowCount,
          createdAt: osqueryResult.timestamp,
        });
        setExecuting(false);
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
  }, [visible, executionId, device?.name]);

  const handleExecute = useCallback(async () => {
    if (!device || !query.trim()) return;

    try {
      setExecuting(true);
      setResult(null);

      // Generate execution ID
      const newExecutionId = crypto.randomUUID();
      setExecutionId(newExecutionId);

      // Send query via SignalR
      await userHubService.executeOSQuery(device.id, query, newExecutionId);

      // Save query to history (async, don't wait)
      saveQueryToHistory({ query }).catch((error) => {
        console.error('Failed to save query to history:', error);
      });
    } catch (error) {
      console.error('Failed to execute OSQuery:', error);
      setExecuting(false);
      setResult({
        id: crypto.randomUUID(),
        executionId: executionId || '',
        deviceId: device.id,
        deviceName: device.name,
        success: false,
        errorMessage: error instanceof Error ? error.message : 'Failed to execute query',
        rawJson: null,
        executionTimeMs: 0,
        rowCount: 0,
        createdAt: new Date().toISOString(),
      });
    }
  }, [device, query, executionId]);

  const handleClear = () => {
    setQuery('');
    setResult(null);
    setExecuting(false);
    setExecutionId(null);
  };

  const handleHide = () => {
    handleClear();
    onHide();
  };

  const footer = (
    <div className="flex justify-content-end gap-2">
      <Button
        label="Clear"
        icon="pi pi-times"
        onClick={handleClear}
        severity="secondary"
        outlined
      />
      <Button
        label="Execute Query"
        icon="pi pi-play"
        onClick={handleExecute}
        disabled={executing || !query.trim() || !device?.online}
        loading={executing}
      />
    </div>
  );

  return (
    <Dialog
      header={`Run OSQuery - ${device?.name || 'Device'}`}
      visible={visible}
      onHide={handleHide}
      style={{ width: '90vw', maxWidth: '1200px' }}
      footer={footer}
      modal
    >
      <div className="flex flex-column gap-3">
        <div>
          <label className="block mb-2 font-semibold">SQL Query</label>
          <MonacoQueryEditor
            value={query}
            onChange={setQuery}
            height="250px"
            readOnly={executing}
          />
        </div>

        {!device?.online && (
          <div className="p-3 surface-100 border-round flex align-items-center gap-2 text-orange-500">
            <i className="pi pi-exclamation-triangle"></i>
            <span>Device is offline. Query execution is disabled.</span>
          </div>
        )}

        {executing && (
          <div className="flex flex-column align-items-center justify-content-center py-5 gap-3">
            <ProgressSpinner style={{ width: '50px', height: '50px' }} />
            <span className="text-color-secondary">Executing query on {device?.name}...</span>
          </div>
        )}

        {result && device && (
          <div>
            <label className="block mb-2 font-semibold">Result</label>
            <OSQueryDeviceResult result={result} deviceName={device.name} />
          </div>
        )}
      </div>
    </Dialog>
  );
};
