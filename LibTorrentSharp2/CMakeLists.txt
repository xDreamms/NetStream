cmake_minimum_required(VERSION 3.18)
project(LibTorrentSharp2 CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ============================================================================
# Paths - adjust these to your environment
# ============================================================================
# Libtorrent source tree (containing include/ and src/ dirs)
set(LIBTORRENT_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../../libtorrent/libtorrent"
    CACHE PATH "Path to libtorrent source root")

# Boost root (header-only for boost filesystem/system on Android)
set(BOOST_ROOT "C:/Libraries/boost_1_87_0"
    CACHE PATH "Path to Boost root")

# ============================================================================
# Libtorrent as a static library (from source)
# ============================================================================
# We build libtorrent from source for Android
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)

# Libtorrent build options
set(deprecated-functions ON CACHE BOOL "" FORCE)
set(python-bindings OFF CACHE BOOL "" FORCE)
set(build_tests OFF CACHE BOOL "" FORCE)
set(build_examples OFF CACHE BOOL "" FORCE)
set(build_tools OFF CACHE BOOL "" FORCE)
set(encryption ON CACHE BOOL "" FORCE)
set(dht ON CACHE BOOL "" FORCE)

# Use Boost headers
set(Boost_INCLUDE_DIR "${BOOST_ROOT}" CACHE PATH "" FORCE)

# Add libtorrent subdirectory
add_subdirectory(${LIBTORRENT_ROOT} libtorrent_build EXCLUDE_FROM_ALL)

# ============================================================================
# Our LibTorrentSharp2 shared library
# ============================================================================
add_library(LibTorrentSharp2 SHARED
    LibTorrentSharp2/libtorrentsharp2.cpp
    LibTorrentSharp2/libtorrentsharp2.h
)

target_include_directories(LibTorrentSharp2 PRIVATE
    ${LIBTORRENT_ROOT}/include
    ${BOOST_ROOT}
)

target_compile_definitions(LibTorrentSharp2 PRIVATE
    LIBTORRENTSHARP2_EXPORTS
    BOOST_ALL_NO_LIB
)

# Link against the static libtorrent
target_link_libraries(LibTorrentSharp2 PRIVATE
    torrent-rasterbar
)

# Android-specific: link log library
if(ANDROID)
    target_link_libraries(LibTorrentSharp2 PRIVATE log)
endif()

# Set output name
set_target_properties(LibTorrentSharp2 PROPERTIES
    OUTPUT_NAME "LibTorrentSharp2"
    PREFIX "lib"
)
