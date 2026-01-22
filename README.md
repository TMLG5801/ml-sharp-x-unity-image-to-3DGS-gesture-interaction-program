Please scroll down for the English version.

## 一、安装与运行指南

本项目采用自动化脚本部署，请严格按照以下步骤操作。

### 1. 前置准备
**必须安装 Python 3.10！** 如果你的电脑没有 Python，或者版本不对，请先安装。

 [点击下载 Python 3.10 (Windows 64位)](https://www.python.org/ftp/python/3.10.11/python-3.10.11-amd64.exe)
 
 **⚠️ 重要：** 安装时务必勾选底部的 **"Add Python 3.10 to PATH"** (添加到环境变量)，否则脚本无法运行！
    (如果不确定是否安装成功，请打开 CMD 输入 `python --version` 检查

**必须安装 Unity6！** 

 [点击下载 Unity6(6000.2.15f1)](https://unity.com/cn/releases/editor/whats-new/6000.2.15f1#installs)

### 2. 一键安装环境
双击项目目录下的 **`install_env.bat`**。
脚本会自动创建虚拟环境并下载所需的 AI 依赖库。
等待出现“环境安装完成”提示后关闭窗口。

### 3. 启动程序
双击 **`start_app.bat`** 即可启动。
* 首次运行时会弹窗提示选择 Unity.exe 的路径，请根据你电脑的实际安装位置选择。


## 二、项目介绍

本项目实现了一个端到端的从单张 RGB 静态图像生成高质量三维场景并进行实时交互的系统。系统利用 3D Gaussian Splatting (3DGS) 技术解决传统 Mesh 重建中的边缘拉伸与体积缺失问题，并通过 MediaPipe 手势识别实现了基于自然用户界面 (NUI) 的无接触交互。

系统采用 B/S (Backend/Frontend) 异构架构

后端 (Python)：基于 Apple ml-sharp 框架进行深度推理与点云生成，利用 plyfile 与 NumPy 进行数据清洗与空间归一化，并通过 UDP 协议传输手势数据。

前端 (Unity)：基于 URP 管线渲染 3DGS 模型，通过自定义 Editor 脚本实现资产自动导入与场景构建，支持鼠标编辑与手势漫游双模式。

主要特性

单图三维化：推荐使用苹果设备拍摄的原图（带有焦距等信息），支持 JPG/PNG 等格式输入，一键生成带有光影体积感的 3DGS 模型 。

全自动工作流：集成了 Python 推理到 Unity 资产导入的全流程，自动处理 PLY 格式转换、坐标系修正 (Flip Y/Mirror X) 与组件挂载 。

多模态交互

编辑模式：鼠标精细控制旋转、平移与 FOV 调整 。

查看模式：基于 MediaPipe 的隔空手势控制（张手旋转、捏合缩放）及 WASD 漫游 。

本地化部署：完全依赖本地 GPU 算力，无需云端 API，支持离线运行 。

## 三、环境依赖

硬件要求

GPU: NVIDIA GPU。

外设: 用于手势识别的普通 USB 摄像头。

软件要求

Unity Engine: Unity6(6000.2.15f1) (项目已经自带 Universal Render Pipeline 插件)。

Unity 插件: Gaussian Splatting for Unity (已经预先导入项目中)。

Python: 3.10+

CUDA Toolkit: 与 PyTorch 版本匹配的版本。

## 四、数据流

<img width="1781" height="2565" alt="System Architecture   Data Flow Diagram" src="https://github.com/user-attachments/assets/fc2b7e16-d0cf-44f1-a3cd-509474fd568d" />

## I. Installation and Running Guide

This project uses an automated deployment script. Please follow the steps below precisely.

### 1. Prerequisites
**Python 3.10 must be installed!** If your computer does not have Python, or the version is incorrect, please install it first.

[Click to download Python 3.10 (Windows 64-bit)](https://www.python.org/ftp/python/3.10.11/python-3.10.11-amd64.exe)

**⚠️ Important:** During installation, be sure to check the **"Add Python 3.10 to PATH"** option at the bottom; otherwise, the script will not run! (If you are unsure whether the installation was successful, please open CMD and enter `python --version` to check.)

**Unity 6 must be installed!**

[Click to download Unity 6 (6000.2.15f1)](https://unity.com/cn/releases/editor/whats-new/6000.2.15f1#installs)

### 2. One-click Environment Installation
Double-click the **`install_env.bat`** file in the project directory.
The script will automatically create a virtual environment and download the necessary AI dependency libraries.
Wait for the "Environment installation complete" message before closing the window.

### 3. Start the Program
Double-click **`start_app.bat`** to start the program.
* The first time you run it, a pop-up window will prompt you to select the path to Unity.exe. Please select the actual installation location on your computer.


## II. Project Introduction

This project implements an end-to-end system that generates high-quality 3D scenes from a single RGB static image and allows for real-time interaction. The system utilizes 3D Gaussian Splatting (3DGS) technology to address the edge stretching and volume loss problems in traditional mesh reconstruction, and achieves contactless interaction based on a natural user interface (NUI) through MediaPipe gesture recognition.

The system uses a B/S (Backend/Frontend) heterogeneous architecture

Backend (Python): Based on the Apple ml-sharp framework for deep inference and point cloud generation, using plyfile and NumPy for data cleaning and spatial normalization, and transmitting gesture data via UDP protocol.

Frontend (Unity): Renders the 3DGS model based on the URP pipeline, automatically imports assets and builds scenes through custom Editor scripts, and supports both mouse editing and gesture-based navigation modes.

Main Features

Single-image 3D reconstruction:  Recommended to use original images taken with Apple devices (with focal length information), supports JPG/PNG and other input formats, and generates a 3DGS model with realistic lighting and volume effects with one click.

Fully automated workflow: Integrates the entire process from Python inference to Unity asset import, automatically handling PLY format conversion, coordinate system correction (Flip Y/Mirror X), and component mounting.

Multi-modal interaction

Editing mode: Precise control of rotation, translation, and FOV adjustment with the mouse.

Viewing mode: Based on MediaPipe Gesture control (hand rotation, pinch-to-zoom) and WASD navigation.

Local deployment: Completely relies on local GPU computing power, no cloud API required, supports offline operation.

## III. Environmental Dependencies (Prerequisites)

Hardware Requirements

GPU: NVIDIA GPU.

Peripherals: A standard USB camera for gesture recognition.

Software Requirements

Unity Engine: Unity6 (6000.2.15f1) (The project already includes the Universal Render Pipeline plugin).

Unity Plugin: Gaussian Splatting for Unity (already pre-imported into the project).

Python: 3.10+

CUDA Toolkit: A version compatible with your PyTorch version.

## IV. Data Flow Diagram

<img width="1781" height="2565" alt="System Architecture   Data Flow Diagram" src="https://github.com/user-attachments/assets/1b622779-549d-497b-b148-871d03e2eafc" />
