#!/bin/bash
set -e

# =============================================================================
# LibTorrentSharp2 macOS Build Script
# Bu scripti macOS üzerinde çalıştır.
# Gereksinimler: Xcode Command Line Tools, CMake, Homebrew
# =============================================================================

echo "=== LibTorrentSharp2 macOS Build Script ==="

# Konfigürasyon
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
LIBTORRENT_SRC="$SCRIPT_DIR/../libtorrent/libtorrent"
WRAPPER_SRC="$SCRIPT_DIR/LibTorrentSharp2"
BUILD_DIR="$SCRIPT_DIR/build-macos"
OUTPUT_DIR="$SCRIPT_DIR/output-macos"

# Mimari tespiti (Intel mi Apple Silicon mı)
ARCH=$(uname -m)
if [ "$ARCH" = "arm64" ]; then
    RID="osx-arm64"
    echo "Tespit edilen mimari: Apple Silicon (arm64)"
else
    RID="osx-x64"
    echo "Tespit edilen mimari: Intel (x86_64)"
fi

# ==== Adım 1: Bağımlılıkları Kur ====
echo ""
echo "=== Adım 1: Bağımlılıkları kontrol et ==="

if ! command -v brew &> /dev/null; then
    echo "HATA: Homebrew kurulu değil. Önce Homebrew kur:"
    echo '/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"'
    exit 1
fi

if ! command -v cmake &> /dev/null; then
    echo "CMake kuruluyor..."
    brew install cmake
fi

# Boost kurulumu
if [ ! -d "$(brew --prefix boost 2>/dev/null)" ]; then
    echo "Boost kuruluyor..."
    brew install boost
fi

# OpenSSL kurulumu
if [ ! -d "$(brew --prefix openssl 2>/dev/null)" ]; then
    echo "OpenSSL kuruluyor..."
    brew install openssl
fi

BOOST_ROOT=$(brew --prefix boost)
OPENSSL_ROOT=$(brew --prefix openssl)

echo "Boost: $BOOST_ROOT"
echo "OpenSSL: $OPENSSL_ROOT"

# ==== Adım 2: libtorrent Derle ====
echo ""
echo "=== Adım 2: libtorrent derleniyor ==="

if [ ! -d "$LIBTORRENT_SRC" ]; then
    echo "HATA: libtorrent kaynak kodu bulunamadı: $LIBTORRENT_SRC"
    echo "libtorrent kaynak kodunu şu konuma koy: $LIBTORRENT_SRC"
    exit 1
fi

LIBTORRENT_BUILD="$BUILD_DIR/libtorrent"
mkdir -p "$LIBTORRENT_BUILD"
cd "$LIBTORRENT_BUILD"

cmake "$LIBTORRENT_SRC" \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_CXX_STANDARD=17 \
    -DCMAKE_OSX_ARCHITECTURES=$ARCH \
    -DBOOST_ROOT="$BOOST_ROOT" \
    -DOPENSSL_ROOT_DIR="$OPENSSL_ROOT" \
    -Dstatic_runtime=ON \
    -Dbuild_examples=OFF \
    -Dbuild_tests=OFF \
    -Dpython-bindings=OFF \
    -DCMAKE_POSITION_INDEPENDENT_CODE=ON

cmake --build . --config Release -j$(sysctl -n hw.ncpu)

LIBTORRENT_LIB="$LIBTORRENT_BUILD"
LIBTORRENT_INCLUDE="$LIBTORRENT_SRC/include"

echo "libtorrent derlendi."

# ==== Adım 3: LibTorrentSharp2 Wrapper Derle ====
echo ""
echo "=== Adım 3: LibTorrentSharp2 wrapper derleniyor ==="

mkdir -p "$OUTPUT_DIR/$RID"

# Shared library olarak derle
clang++ -std=c++17 -shared -fPIC -fvisibility=hidden \
    -DLIBTORRENTSHARP2_EXPORTS \
    -O2 \
    -arch $ARCH \
    -I"$LIBTORRENT_INCLUDE" \
    -I"$BOOST_ROOT/include" \
    -I"$OPENSSL_ROOT/include" \
    -L"$LIBTORRENT_BUILD" \
    -L"$BOOST_ROOT/lib" \
    -L"$OPENSSL_ROOT/lib" \
    -o "$OUTPUT_DIR/$RID/libLibTorrentSharp2.dylib" \
    "$WRAPPER_SRC/libtorrentsharp2.cpp" \
    -ltorrent-rasterbar \
    -lboost_system \
    -lssl -lcrypto \
    -lpthread \
    -install_name @rpath/libLibTorrentSharp2.dylib

# rpath ayarla - libtorrent'i de dylib'in yanında arasın
install_name_tool -add_rpath @loader_path "$OUTPUT_DIR/$RID/libLibTorrentSharp2.dylib"

echo "LibTorrentSharp2 derlendi: $OUTPUT_DIR/$RID/libLibTorrentSharp2.dylib"

# Bağımlılıkları kontrol et
echo ""
echo "=== Bağımlılık kontrolü ==="
otool -L "$OUTPUT_DIR/$RID/libLibTorrentSharp2.dylib"

# ==== Adım 4: Gerekli kütüphaneleri kopyala ====
echo ""
echo "=== Adım 4: Bağımlı kütüphaneler kopyalanıyor ==="

# libtorrent shared lib varsa kopyala
if [ -f "$LIBTORRENT_BUILD/libtorrent-rasterbar.dylib" ]; then
    cp "$LIBTORRENT_BUILD/libtorrent-rasterbar.dylib" "$OUTPUT_DIR/$RID/"
    echo "libtorrent-rasterbar.dylib kopyalandı"
elif [ -f "$LIBTORRENT_BUILD/libtorrent-rasterbar.2.dylib" ]; then
    cp "$LIBTORRENT_BUILD/libtorrent-rasterbar.2.dylib" "$OUTPUT_DIR/$RID/libtorrent-rasterbar.dylib"
    echo "libtorrent-rasterbar.dylib kopyalandı"
fi

echo ""
echo "=== BUILD TAMAMLANDI ==="
echo "Çıktı dizini: $OUTPUT_DIR/$RID/"
echo ""
echo "Sonraki adım: Bu dosyaları NetStream.macOS/native/$RID/ dizinine kopyala:"
echo "  cp $OUTPUT_DIR/$RID/*.dylib /path/to/NetStream.macOS/native/$RID/"
echo ""
echo ".NET uygulamasını derlemek için:"
echo "  cd /path/to/NetStream.macOS"
echo "  dotnet publish -c Release -r $RID --self-contained"
