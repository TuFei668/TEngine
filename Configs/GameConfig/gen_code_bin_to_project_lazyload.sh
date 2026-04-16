#!/bin/bash

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

export WORKSPACE="$(realpath ../../)"
export LUBAN_DLL="${WORKSPACE}/Tools/Luban/Luban.dll"
export CONF_ROOT="$(pwd)"
export DATA_OUTPATH="${WORKSPACE}/UnityProject/Assets/AssetRaw/Configs/bytes/"
export CODE_OUTPATH="${WORKSPACE}/UnityProject/Assets/GameScripts/HotFix/GameProto/GameConfig/"

if command -v dotnet >/dev/null 2>&1; then
    DOTNET_CMD="$(command -v dotnet)"
elif [ -x "/usr/local/share/dotnet/dotnet" ]; then
    DOTNET_CMD="/usr/local/share/dotnet/dotnet"
elif [ -x "/opt/homebrew/share/dotnet/dotnet" ]; then
    DOTNET_CMD="/opt/homebrew/share/dotnet/dotnet"
else
    echo "未找到 dotnet，请先安装 .NET SDK，或将 dotnet 加入 PATH"
    exit 1
fi

echo "使用 dotnet: ${DOTNET_CMD}"

cp -R "${CONF_ROOT}/CustomTemplate/ConfigSystem.cs" \
   "${WORKSPACE}/UnityProject/Assets/GameScripts/HotFix/GameProto/ConfigSystem.cs"
cp -R "${CONF_ROOT}/CustomTemplate/ExternalTypeUtil.cs" \
    "${WORKSPACE}/UnityProject/Assets/GameScripts/HotFix/GameProto/ExternalTypeUtil.cs"

"${DOTNET_CMD}" "${LUBAN_DLL}" \
    -t client \
    -c cs-bin \
    -d bin \
    --conf "${CONF_ROOT}/luban.conf" \
    --customTemplateDir "${CONF_ROOT}/CustomTemplate/CustomTemplate_Client_LazyLoad" \
    -x code.lineEnding=crlf \
    -x outputCodeDir="${CODE_OUTPATH}" \
    -x outputDataDir="${DATA_OUTPATH}"
