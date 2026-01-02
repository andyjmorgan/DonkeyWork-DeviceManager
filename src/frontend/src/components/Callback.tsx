import { useEffect, useState, useRef } from 'react';
import { Card } from 'primereact/card';
import { ProgressSpinner } from 'primereact/progressspinner';
import { Message } from 'primereact/message';
import { Button } from 'primereact/button';
import './Callback.css';

function Callback() {
  const [error, setError] = useState<string | null>(null);
  const hasProcessed = useRef(false);

  useEffect(() => {
    const handleCallback = async () => {
      // Prevent double execution in React StrictMode
      if (hasProcessed.current) return;
      hasProcessed.current = true;
      const urlParams = new URLSearchParams(window.location.search);
      const code = urlParams.get('code');
      const state = urlParams.get('state');
      const iss = urlParams.get('iss');
      const error = urlParams.get('error');

      if (error) {
        setError(`Authentication error: ${error}`);
        return;
      }

      if (!code) {
        setError('No authorization code received');
        return;
      }

      try {
        const response = await fetch('/api/auth/callback', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            code,
            state,
            iss,
            redirectUri: window.location.origin + '/callback',
          }),
        });

        if (!response.ok) {
          const errorData = await response.json().catch(() => ({}));
          throw new Error(errorData.message || errorData.error || `Authentication failed (${response.status})`);
        }

        const data = await response.json();

        // Store tokens
        if (data.accessToken) {
          localStorage.setItem('access_token', data.accessToken);
        }
        if (data.refreshToken) {
          localStorage.setItem('refresh_token', data.refreshToken);
        }

        // Redirect to main application
        window.location.href = '/';
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Authentication failed');
      }
    };

    handleCallback();
  }, []);

  const handleTryAgain = () => {
    window.location.href = '/';
  };

  return (
    <div className="callback-container">
      <Card className="callback-card">
        <div className="callback-content">
          <img src="/donkeywork.png" alt="DonkeyWork Logo" className="callback-logo" />
          {error ? (
            <>
              <Message severity="error" text={error} />
              <Button
                label="Try Again"
                icon="pi pi-refresh"
                onClick={handleTryAgain}
                className="callback-button"
              />
            </>
          ) : (
            <>
              <ProgressSpinner />
              <p className="callback-text">Completing authentication...</p>
            </>
          )}
        </div>
      </Card>
    </div>
  );
}

export default Callback;
