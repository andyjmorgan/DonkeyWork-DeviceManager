import { useState, useEffect, useRef } from 'react';
import Login from './components/Login';
import Callback from './components/Callback';
import Dashboard from './components/Dashboard';
import { ProgressSpinner } from 'primereact/progressspinner';
import { authenticatedFetch } from './utils/apiClient';
import './App.css'

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const path = window.location.pathname;
  const hasValidated = useRef(false);

  useEffect(() => {
    const validateAuth = async () => {
      // Prevent double execution in React StrictMode
      if (hasValidated.current) return;
      hasValidated.current = true;
      // Skip validation on callback page
      if (path === '/callback') {
        setIsLoading(false);
        return;
      }

      const accessToken = localStorage.getItem('access_token');

      if (!accessToken) {
        setIsAuthenticated(false);
        setIsLoading(false);
        return;
      }

      try {
        const response = await authenticatedFetch('/api/profile/me');

        if (response.ok) {
          setIsAuthenticated(true);
        } else {
          // Token invalid even after refresh attempt, clear storage
          localStorage.removeItem('access_token');
          localStorage.removeItem('refresh_token');
          setIsAuthenticated(false);
        }
      } catch (err) {
        console.error('Auth validation failed:', err);
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        setIsAuthenticated(false);
      } finally {
        setIsLoading(false);
      }
    };

    validateAuth();
  }, [path]);

  if (path === '/callback') {
    return <Callback />;
  }

  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh' }}>
        <ProgressSpinner />
      </div>
    );
  }

  return isAuthenticated ? <Dashboard /> : <Login />;
}

export default App
