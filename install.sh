#!/bin/sh
# DonkeyWork Device Manager Client - One-Liner Installer
# Downloads the latest release and runs the platform-specific install script
#
# Usage:
#   curl -sfL https://raw.githubusercontent.com/YOUR_ORG/DonkeyWork-DeviceManager/main/install.sh | sudo sh -
#
# With custom API URL:
#   curl -sfL https://raw.githubusercontent.com/YOUR_ORG/DonkeyWork-DeviceManager/main/install.sh | sudo DEVICE_MANAGER_API_URL=https://your-api.example.com sh -
#
# Install specific version:
#   curl -sfL https://raw.githubusercontent.com/YOUR_ORG/DonkeyWork-DeviceManager/main/install.sh | sudo INSTALL_VERSION=v1.0.0 sh -

set -e

# Configuration
GITHUB_REPO="${GITHUB_REPO:-YOUR_ORG/DonkeyWork-DeviceManager}"
INSTALL_VERSION="${INSTALL_VERSION:-latest}"
API_BASE_URL="${DEVICE_MANAGER_API_URL:-https://devicemanager.donkeywork.dev}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Helper functions
info() {
    printf "${CYAN}[INFO]${NC} %s\n" "$1"
}

success() {
    printf "${GREEN}[SUCCESS]${NC} %s\n" "$1"
}

warn() {
    printf "${YELLOW}[WARN]${NC} %s\n" "$1"
}

error() {
    printf "${RED}[ERROR]${NC} %s\n" "$1"
    exit 1
}

# Check if running as root
if [ "$(id -u)" -ne 0 ]; then
    error "This script must be run as root (use sudo)"
fi

# Detect OS and architecture
detect_platform() {
    OS="$(uname -s)"
    ARCH="$(uname -m)"

    case "$OS" in
        Linux*)
            OS_NAME="linux"
            case "$ARCH" in
                x86_64|amd64)
                    ARCH_NAME="x64"
                    PLATFORM_NAME="Linux"
                    SCRIPT_NAME="install.sh"
                    ;;
                aarch64|arm64)
                    error "Linux ARM64 is not currently supported. Supported: linux-x64, osx-arm64, osx-x64, win-x64"
                    ;;
                *)
                    error "Unsupported Linux architecture: $ARCH"
                    ;;
            esac
            ;;
        Darwin*)
            OS_NAME="osx"
            case "$ARCH" in
                x86_64|amd64)
                    error "macOS x64 is not currently supported. Supported: linux-x64, osx-arm64, win-x64"
                    ;;
                arm64)
                    ARCH_NAME="arm64"
                    PLATFORM_NAME="macOS"
                    SCRIPT_NAME="install.sh"
                    ;;
                *)
                    error "Unsupported macOS architecture: $ARCH"
                    ;;
            esac
            ;;
        *)
            error "Unsupported operating system: $OS. This script supports Linux and macOS. For Windows, download the release and run install.ps1"
            ;;
    esac

    RUNTIME="${OS_NAME}-${ARCH_NAME}"
    info "Detected platform: $PLATFORM_NAME ($RUNTIME)"
}

# Get download URL for release
get_download_url() {
    if [ "$INSTALL_VERSION" = "latest" ]; then
        info "Fetching latest release information..."
        GITHUB_API="https://api.github.com/repos/${GITHUB_REPO}/releases/latest"
    else
        info "Fetching release information for $INSTALL_VERSION..."
        GITHUB_API="https://api.github.com/repos/${GITHUB_REPO}/releases/tags/${INSTALL_VERSION}"
    fi

    # Fetch release info
    RELEASE_INFO=$(curl -sfL "$GITHUB_API" 2>/dev/null) || error "Failed to fetch release from GitHub. Check that the repository exists and has releases."

    # Extract version
    VERSION=$(echo "$RELEASE_INFO" | grep '"tag_name":' | sed -E 's/.*"tag_name": *"([^"]+)".*/\1/' | head -n1)

    if [ -z "$VERSION" ]; then
        error "Could not parse version from GitHub API response"
    fi

    # Build expected filename based on CI/CD workflow naming
    EXPECTED_FILENAME="DonkeyWorkDeviceManager-DeviceClient-${PLATFORM_NAME}-${VERSION}.zip"

    # Extract download URL for our platform
    DOWNLOAD_URL=$(echo "$RELEASE_INFO" | grep -o "https://github.com/${GITHUB_REPO}/releases/download/${VERSION}/${EXPECTED_FILENAME}" | head -n1)

    if [ -z "$DOWNLOAD_URL" ]; then
        error "Could not find release asset for $RUNTIME. Expected: $EXPECTED_FILENAME"
    fi

    info "Found version: $VERSION"
    info "Download URL: $DOWNLOAD_URL"
}

# Download and extract release
download_release() {
    info "Downloading release..."

    # Create temp directory
    TEMP_DIR=$(mktemp -d)
    DOWNLOAD_FILE="$TEMP_DIR/device-client.zip"

    # Download release
    if ! curl -fsSL "$DOWNLOAD_URL" -o "$DOWNLOAD_FILE"; then
        rm -rf "$TEMP_DIR"
        error "Failed to download release"
    fi

    success "Downloaded release $VERSION"

    info "Extracting archive..."

    # Extract archive
    if ! unzip -q "$DOWNLOAD_FILE" -d "$TEMP_DIR/extracted"; then
        rm -rf "$TEMP_DIR"
        error "Failed to extract archive. Ensure 'unzip' is installed."
    fi

    EXTRACT_DIR="$TEMP_DIR/extracted"
    success "Extracted archive"
}

# Run platform install script
run_install_script() {
    info "Running platform install script..."

    INSTALL_SCRIPT="$EXTRACT_DIR/scripts/$SCRIPT_NAME"

    if [ ! -f "$INSTALL_SCRIPT" ]; then
        rm -rf "$TEMP_DIR"
        error "Install script not found: $INSTALL_SCRIPT"
    fi

    chmod +x "$INSTALL_SCRIPT"

    # Export API URL for install script to use
    export API_BASE_URL

    # Run the install script with inherited environment
    if ! "$INSTALL_SCRIPT"; then
        rm -rf "$TEMP_DIR"
        error "Installation failed"
    fi
}

# Cleanup
cleanup() {
    if [ -n "$TEMP_DIR" ] && [ -d "$TEMP_DIR" ]; then
        info "Cleaning up temporary files..."
        rm -rf "$TEMP_DIR"
    fi
}

# Main installation flow
main() {
    printf "${CYAN}"
    cat << "EOF"
========================================
DonkeyWork Device Manager Client
One-Liner Installer
========================================
EOF
    printf "${NC}\n"

    # Detect platform
    detect_platform

    # Get download URL
    get_download_url

    # Download and extract
    download_release

    # Run platform-specific installer
    run_install_script

    # Cleanup
    cleanup

    printf "\n${CYAN}========================================${NC}\n"
    printf "${GREEN}Installation Complete!${NC}\n"
    printf "${CYAN}========================================${NC}\n\n"

    success "Device client has been installed and started as a service"

    printf "\n${YELLOW}View logs:${NC}\n"
    case "$OS_NAME" in
        linux)
            printf "  journalctl -u donkeywork-device-client -f\n"
            ;;
        osx)
            printf "  tail -f /var/log/donkeywork-device-client.log\n"
            ;;
    esac
    printf "\n"
}

# Run with error handling
main "$@"
