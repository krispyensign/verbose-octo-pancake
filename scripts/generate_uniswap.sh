#!/bin/bash

cd vendor/uniswap-v4-periphery || exit

if [ ! -d foundry-out ]; then
    foundry build
fi
cd ../../

mkdir -p gen/
cd gen || exit

if ! type Nethereum.Generator.Console &> /dev/null; then
    dotnet tool install -g Nethereum.Generator.Console
fi

# scrape the output of foundry
jq '.abi' ../vendor/uniswap-v4-periphery/foundry-out/IV4Quoter.sol/IV4Quoter.json > IV4Quoter.sol.abi

# use the nethereum codegen
Nethereum.Generator.Console generate from-abi -abi ./IV4Quoter.sol.abi  -o . -ns Uniswap.ABI

# patch everything
find . -iname '*.cs' -print0 | xargs -0 sed -i 's/\.sol/sol/g'