# ================================================================================================
#  DS3OS
#  Copyright (C) 2021 Tim Leonard
# ================================================================================================
cmake_minimum_required(VERSION 3.8)

# Options
option(BUILD_SHARED_LIBS "Build using shared libraries" ON)

# Environment settings that all projects inherit.
set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
set(CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/tools/build/cmake_includes;${CMAKE_MODULE_PATH}")
set(ENV_ROOT_PATH "${CMAKE_CURRENT_SOURCE_DIR}")

# Common configuration for all projects.
include(common)

# Make native project
project(native C)

# Add projects
add_subdirectory(aes_modes)
add_subdirectory(aes)
add_subdirectory(openssl)