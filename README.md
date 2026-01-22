## 安装与运行指南

本项目采用自动化脚本部署，请严格按照以下步骤操作。

### 1. 前置准备

硬件要求

GPU: NVIDIA GPU。

Webcam: 用于手势识别的普通 USB 摄像头。

**必须安装 Python 3.10！** 如果你的电脑没有 Python，或者版本不对，请先安装。

 [点击下载 Python 3.10 (Windows 64位)](https://www.python.org/ftp/python/3.10.11/python-3.10.11-amd64.exe)
 
 **⚠️ 重要：** 安装时务必勾选底部的 **"Add Python 3.10 to PATH"** (添加到环境变量)，否则脚本无法运行！
    (如果不确定是否安装成功，请打开 CMD 输入 `python --version` 检查)

Unity Engine: Unity6(6000.2.15f1) (需安装 Universal Render Pipeline)。

Unity 插件: Gaussian Splatting for Unity (需预先导入项目中)。

CUDA Toolkit: 与 PyTorch 版本匹配的版本。

### 2. 一键安装环境
双击项目目录下的 **`install_env.bat`**。
脚本会自动创建虚拟环境并下载所需的 AI 依赖库。
等待出现“环境安装完成”提示后关闭窗口。

### 3. 启动程序
双击 **`start_app.bat`** 即可启动。
* 首次运行时会弹窗提示选择 Unity.exe 的路径，请根据你电脑的实际安装位置选择。

## 项目介绍

本项目实现了一个端到端的从单张 RGB 静态图像生成高质量三维场景并进行实时交互的系统。系统利用 3D Gaussian Splatting (3DGS) 技术解决传统 Mesh 重建中的边缘拉伸与体积缺失问题，并通过 MediaPipe 手势识别实现了基于自然用户界面 (NUI) 的无接触交互。

系统采用 B/S (Backend/Frontend) 异构架构：

后端 (Python)：基于 Apple ml-sharp 框架进行深度推理与点云生成，利用 plyfile 与 NumPy 进行数据清洗与空间归一化，并通过 UDP 协议传输手势数据。

前端 (Unity)：基于 URP 管线渲染 3DGS 模型，通过自定义 Editor 脚本实现资产自动导入与场景构建，支持鼠标编辑与手势漫游双模式。

主要特性

单图三维化：推荐使用苹果设备拍摄的原图（带有焦距等信息），支持 JPG/PNG 等格式输入，一键生成带有光影体积感的 3DGS 模型 。

全自动工作流：集成了 Python 推理到 Unity 资产导入的全流程，自动处理 PLY 格式转换、坐标系修正 (Flip Y/Mirror X) 与组件挂载 。

多模态交互

编辑模式：鼠标精细控制旋转、平移与 FOV 调整 。

查看模式：基于 MediaPipe 的隔空手势控制（张手旋转、捏合缩放）及 WASD 漫游 。

本地化部署：完全依赖本地 GPU 算力，无需云端 API，支持离线运行 。

## 数据流

<img width="1781" height="2565" alt="System Architecture   Data Flow Diagram" src="https://github.com/user-attachments/assets/c901ac36-5233-40d6-8f0d-30ed18af507a" />


## Installation Guide

This project uses an automated script for deployment. Please strictly follow the steps below.

### 1. Prerequisites

Hardware Requirements

GPU: NVIDIA GPU.

Webcam: Standard USB webcam for gesture recognition.

**Python 3.10 is required!** If your computer does not have Python, or the version is incorrect, please install it first.

[Click to download Python 3.10 (Windows 64-bit)](https://www.python.org/ftp/python/3.10.11/python-3.10.11-amd64.exe)

**⚠️ Important:** During installation, be sure to check the box at the bottom for **"Add Python 3.10 to PATH"** (add to environment variables), otherwise the script will not run!

(If you are unsure whether the installation was successful, please open CMD and type `python --version` to check.)

Unity Engine: Unity 6 (6000.2.15f1) (Universal Render Pipeline required).

Unity Plugin: Gaussian Splatting for Unity (must be pre-imported into your project).

CUDA Toolkit: Version compatible with PyTorch.

### 2. One-Click Environment Installation
Double-click the **`install_env.bat` file in the project directory.

The script will automatically create a virtual environment and download the required AI dependency libraries.

Wait for the "Environment installation complete" message to appear, then close the window.

### 3. Starting the Program

Double-click **`start_app.bat`** to start the program.

* Upon first run, a pop-up window will prompt you to select the path to Unity.exe. Please select the path according to your computer's actual installation location.


## Introduction

This project implements an end-to-end system for generating high-quality 3D scenes from a single RGB static image and enabling real-time interaction. The system utilizes 3D Gaussian Splatting (3DGS) technology to solve the edge stretching and volume loss problems in traditional mesh reconstruction, and achieves contactless interaction based on a Natural User Interface (NUI) through MediaPipe gesture recognition.

The system adopts a B/S (Backend/Frontend) heterogeneous architecture:

Backend (Python): Based on the Apple ml-sharp framework for deep inference and point cloud generation, using plyfile and NumPy for data cleaning and spatial normalization, and transmitting gesture data via UDP protocol.

Frontend (Unity): Based on the URP pipeline to render 3DGS models, using a custom Editor script to achieve automatic asset import and scene construction, supporting both mouse editing and gesture roaming modes.

Key Features:

Single Image 3D Generation: Recommends using original images taken with Apple devices (containing focal length and other information), supports JPG/PNG input formats, and generates 3DGS models with lighting and volume with a single click.

Fully Automated Workflow: Integrates the entire process from Python inference to Unity asset import, automatically handling PLY format conversion, coordinate system correction (Flip Y/Mirror X), and component mounting.

Multimodal Interaction:

Edit Mode: Fine-grained mouse control of rotation, translation, and FOV adjustment.

Viewing Mode: MediaPipe-based air gesture control (open hand to rotate, pinch to zoom) and WASD navigation.

Local Deployment: Entirely reliant on local GPU computing power, requiring no cloud API, and supports offline operation.

## Data flow diagram

<img width="1781" height="2565" alt="System Architecture   Data Flow Diagram" src="https://github.com/user-attachments/assets/69b186fb-fe4e-4ec6-84cb-fd89f7b72022" />
