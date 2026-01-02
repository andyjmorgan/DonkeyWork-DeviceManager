import { Card } from 'primereact/card';
import { Button } from 'primereact/button';
import { getKeycloakLoginUrl } from '../config/keycloak';
import './Login.css';

function Login() {
  const handleLogin = () => {
    const redirectUri = window.location.origin + '/callback';
    const loginUrl = getKeycloakLoginUrl(redirectUri);
    window.location.href = loginUrl;
  };

  return (
    <div className="login-container">
      <Card className="login-card">
        <div className="login-content">
          <img src="/donkeywork.png" alt="DonkeyWork Logo" className="login-logo" />
          <p className="login-text">Click here to login</p>
          <Button
            label="Login"
            icon="pi pi-sign-in"
            onClick={handleLogin}
            className="login-button"
          />
        </div>
      </Card>
    </div>
  );
}

export default Login;
