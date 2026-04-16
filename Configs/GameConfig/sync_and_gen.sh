#!/bin/bash

set -e

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

PYTHON_CMD=""
if [ -x ".venv_xlsx/bin/python" ]; then
    PYTHON_CMD=".venv_xlsx/bin/python"
elif command -v python3 >/dev/null 2>&1; then
    PYTHON_CMD="$(command -v python3)"
else
    echo "未找到可用的 Python3，请先安装 Python 或创建 .venv_xlsx"
    exit 1
fi

echo "使用 python: ${PYTHON_CMD}"
echo "开始同步 __tables__.csv -> __tables__.xlsx"
"${PYTHON_CMD}" sync_tables_csv_to_xlsx.py

echo "开始执行 Luban 生成"
sh "./gen_code_bin_to_project_lazyload.sh"

echo "同步并生成完成"
