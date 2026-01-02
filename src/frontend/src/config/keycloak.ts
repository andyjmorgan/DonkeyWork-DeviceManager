export const keycloakConfig = {
  url: 'https://auth.donkeywork.dev',
  realm: 'DeviceManager',
  clientId: 'DeviceManager',
};

const generateState = (): string => {
  const array = new Uint8Array(32);
  crypto.getRandomValues(array);
  return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
};

export const getKeycloakLoginUrl = (redirectUri: string): string => {
  const { url, realm, clientId } = keycloakConfig;
  const state = generateState();

  // Store state for verification
  sessionStorage.setItem('oauth_state', state);

  const params = new URLSearchParams({
    client_id: clientId,
    redirect_uri: redirectUri,
    response_type: 'code',
    scope: 'openid',
    state,
  });

  return `${url}/realms/${realm}/protocol/openid-connect/auth?${params.toString()}`;
};
