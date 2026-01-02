// Import theme URLs at build time
import sohoDarkTheme from 'primereact/resources/themes/soho-dark/theme.css?url';
import sohoLightTheme from 'primereact/resources/themes/soho-light/theme.css?url';

export const loadTheme = (theme: 'dark' | 'light') => {
  // Update color-scheme
  document.documentElement.style.colorScheme = theme === 'dark' ? 'dark' : 'light';

  // Store preference
  localStorage.setItem('theme', theme);

  // Get or create theme link element
  let themeLink = document.getElementById('app-theme') as HTMLLinkElement;

  if (!themeLink) {
    themeLink = document.createElement('link');
    themeLink.id = 'app-theme';
    themeLink.rel = 'stylesheet';
    themeLink.type = 'text/css';
    // Insert as first link to ensure it loads before other styles
    const firstLink = document.head.querySelector('link');
    if (firstLink) {
      document.head.insertBefore(themeLink, firstLink);
    } else {
      document.head.appendChild(themeLink);
    }
  }

  // Update the href to switch themes
  themeLink.href = theme === 'dark' ? sohoDarkTheme : sohoLightTheme;
};

export const getStoredTheme = (): 'dark' | 'light' => {
  const stored = localStorage.getItem('theme');
  return stored === 'light' ? 'light' : 'dark';
};
