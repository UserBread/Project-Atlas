cmake_minimum_required(VERSION 3.20)

project(ProjectAtlas VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_executable(atlas
    src/main.cpp
)

target_include_directories(atlas PRIVATE
    include
)

if (MSVC)
    target_compile_options(atlas PRIVATE /W4 /permissive-)
else()
    target_compile_options(atlas PRIVATE -Wall -Wextra -Wpedantic)
endif()
