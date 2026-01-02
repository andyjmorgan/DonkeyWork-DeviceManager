import { useState, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { Avatar } from 'primereact/avatar';
import { Menu } from 'primereact/menu';
import { LayoutDashboard, Building2, DoorOpen, Search, Menu as MenuIcon, X, ChevronLeft, ChevronRight } from 'lucide-react';
import { loadTheme, getStoredTheme } from '../../utils/theme';
import './Layout.css';

interface LayoutProps {
  children: React.ReactNode;
}

interface MenuItem {
  label: string;
  icon: React.ReactNode;
  path: string;
}

function Layout({ children }: LayoutProps) {
  const [theme, setTheme] = useState<'dark' | 'light'>(getStoredTheme());
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);
  const menuRef = useRef<Menu>(null);
  const navigate = useNavigate();
  const location = useLocation();

  const menuItems: MenuItem[] = [
    {
      label: 'Dashboard',
      icon: <LayoutDashboard size={20} />,
      path: '/',
    },
    {
      label: 'Buildings',
      icon: <Building2 size={20} />,
      path: '/buildings',
    },
    {
      label: 'Rooms',
      icon: <DoorOpen size={20} />,
      path: '/rooms',
    },
    {
      label: 'OSQuery',
      icon: <Search size={20} />,
      path: '/osquery',
    },
  ];

  const handleThemeToggle = () => {
    const newTheme = theme === 'dark' ? 'light' : 'dark';
    setTheme(newTheme);
    loadTheme(newTheme);
  };

  const handleLogout = () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    window.location.href = '/';
  };

  const handleMenuItemClick = (path: string) => {
    navigate(path);
    setMobileSidebarOpen(false);
  };

  const userMenuItems = [
    {
      label: 'Theme',
      icon: theme === 'dark' ? 'pi pi-sun' : 'pi pi-moon',
      command: handleThemeToggle,
    },
    {
      separator: true,
    },
    {
      label: 'Logout',
      icon: 'pi pi-sign-out',
      command: handleLogout,
    },
  ];

  return (
    <div className="layout">
      {/* Top Bar */}
      <div className="layout-topbar">
        <div className="topbar-left">
          <button
            className="burger-button"
            onClick={() => setMobileSidebarOpen(!mobileSidebarOpen)}
            aria-label="Toggle menu"
          >
            {mobileSidebarOpen ? <X size={24} /> : <MenuIcon size={24} />}
          </button>
          <img src="/donkeywork.png" alt="DonkeyWork" className="topbar-logo" />
          <span className="topbar-title">Device Manager</span>
        </div>
        <div className="topbar-right">
          <Avatar
            icon="pi pi-user"
            shape="circle"
            style={{ cursor: 'pointer' }}
            onClick={(e) => menuRef.current?.toggle(e)}
          />
          <Menu model={userMenuItems} popup ref={menuRef} />
        </div>
      </div>

      {/* Sidebar */}
      <div
        className={`layout-sidebar ${sidebarCollapsed ? 'collapsed' : ''} ${
          mobileSidebarOpen ? 'mobile-open' : ''
        }`}
      >
        <div className="sidebar-content">
          {/* Desktop Toggle */}
          <button
            className="sidebar-toggle desktop-only"
            onClick={() => setSidebarCollapsed(!sidebarCollapsed)}
            aria-label={sidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          >
            {sidebarCollapsed ? <ChevronRight size={20} /> : <ChevronLeft size={20} />}
          </button>

          {/* Menu Items */}
          <nav className="sidebar-nav">
            {menuItems.map((item) => {
              const isActive = location.pathname === item.path;
              return (
                <button
                  key={item.path}
                  className={`sidebar-item ${isActive ? 'active' : ''}`}
                  onClick={() => handleMenuItemClick(item.path)}
                  title={sidebarCollapsed ? item.label : undefined}
                >
                  <span className="sidebar-item-icon">{item.icon}</span>
                  <span className="sidebar-item-label">{item.label}</span>
                </button>
              );
            })}
          </nav>
        </div>
      </div>

      {/* Mobile Overlay */}
      {mobileSidebarOpen && (
        <div
          className="layout-overlay"
          onClick={() => setMobileSidebarOpen(false)}
        />
      )}

      {/* Main Content */}
      <div className="layout-content">
        {children}
      </div>
    </div>
  );
}

export default Layout;
