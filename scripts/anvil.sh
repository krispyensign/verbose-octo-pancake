#!/bin/bash

decimal=$((16#$(curl \
    -X POST \
    https://base.llamarpc.com \
    -H "Content-Type: application/json" \
    -d '{"jsonrpc":"2.0","method":"eth_blockNumber","params":[],"id":8453}' \
    | jq '.result' -r \
    | sed -e 's/0x//g'\
)))
echo $decimal
anvil --fork-url https://base.llamarpc.com --fork-chain-id 8453 --fork-block-number 27872225 --chain-id 8453 --no-rate-limit