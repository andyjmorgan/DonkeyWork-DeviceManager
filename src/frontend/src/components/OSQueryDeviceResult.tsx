import { useState } from 'react';
import { Panel } from 'primereact/panel';
import { Button } from 'primereact/button';
import { Tag } from 'primereact/tag';
import { JsonView, defaultStyles } from 'react-json-view-lite';
import 'react-json-view-lite/dist/index.css';
import type { OSQueryExecutionResultResponse } from '../types/osquery';

interface OSQueryDeviceResultProps {
  result: OSQueryExecutionResultResponse;
  deviceName: string;
}

export const OSQueryDeviceResult: React.FC<OSQueryDeviceResultProps> = ({
  result,
  deviceName,
}) => {
  const [collapsed, setCollapsed] = useState(true);

  const handleDownloadJson = () => {
    if (!result.rawJson) return;

    const blob = new Blob([result.rawJson], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `osquery-${deviceName}-${result.deviceId}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const headerTemplate = () => {
    return (
      <div className="flex align-items-center justify-content-between">
        <div className="flex align-items-center gap-2">
          <span className="font-semibold">{deviceName}</span>
          {result.success ? (
            <Tag severity="success" value="Success" icon="pi pi-check" />
          ) : (
            <Tag severity="danger" value="Failed" icon="pi pi-times" />
          )}
          <span className="text-sm text-color-secondary">
            {result.executionTimeMs}ms · {result.rowCount} rows
          </span>
        </div>
        <div className="flex gap-2">
          {result.success && result.rawJson && (
            <Button
              icon="pi pi-download"
              size="small"
              text
              onClick={(e) => {
                e.stopPropagation();
                handleDownloadJson();
              }}
              tooltip="Download JSON"
              tooltipOptions={{ position: 'top' }}
            />
          )}
        </div>
      </div>
    );
  };

  const parseJsonSafely = (json: string | null): any => {
    if (!json) return null;
    try {
      return JSON.parse(json);
    } catch {
      return null;
    }
  };

  const jsonData = parseJsonSafely(result.rawJson);

  return (
    <Panel
      header={headerTemplate()}
      toggleable
      collapsed={collapsed}
      onToggle={(e) => setCollapsed(e.value)}
      className="mb-2"
    >
      {result.success ? (
        jsonData ? (
          <div style={{ maxHeight: '400px', overflow: 'auto', padding: '1rem', background: '#1e1e1e', borderRadius: '6px' }}>
            <JsonView data={jsonData} style={defaultStyles} />
          </div>
        ) : (
          <div className="text-center py-3 text-color-secondary">
            No data returned
          </div>
        )
      ) : (
        <div className="p-3 surface-100 border-round">
          <div className="flex align-items-center gap-2 text-red-500">
            <i className="pi pi-exclamation-triangle"></i>
            <span className="font-semibold">Error:</span>
          </div>
          <p className="mt-2 mb-0">{result.errorMessage || 'Unknown error'}</p>
        </div>
      )}
    </Panel>
  );
};
