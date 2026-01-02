import { useState, useEffect, useRef } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Login from './components/Login';
import Callback from './components/Callback';
import Dashboard from './components/Dashboard';
import Buildings from './pages/Buildings';
import Rooms from './pages/Rooms';
import OSQuery from './pages/OSQuery';
import Layout from './components/Layout/Layout';
import { ProgressSpinner } from 'primereact/progressspinner';
import { authenticatedFetch } from './utils/apiClient';
import './App.css'

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const hasValidated = useRef(false);

  useEffect(() => {
    const validateAuth = async () => {
      // Prevent double execution in React StrictMode
      if (hasValidated.current) return;
      hasValidated.current = true;

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
  }, []);

  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh' }}>
        <ProgressSpinner />
      </div>
    );
  }

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/callback" element={<Callback />} />
        {isAuthenticated ? (
          <Route path="/*" element={
            <Layout>
              <Routes>
                <Route path="/" element={<Dashboard />} />
                <Route path="/buildings" element={<Buildings />} />
                <Route path="/rooms" element={<Rooms />} />
                <Route path="/osquery" element={<OSQuery />} />
                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </Layout>
          } />
        ) : (
          <>
            <Route path="/" element={<Login />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </>
        )}
      </Routes>
    </BrowserRouter>
  );
}

export default App
