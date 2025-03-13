# Public Ethereum node URL
NODE_URL="https://rpc.ankr.com/eth"
# DAI token contract address
TOKEN_CONTRACT="0x6B175474E89094C44Da98b954EedeAC495271d0F"
# Token holder address
TOKEN_HOLDER="075e72a5eDf65F0A5f44699c7654C1a76941Ddc8"

# JSON-RPC request payload
PAYLOAD=$(cat << EOM
{
  "jsonrpc": "2.0",
  "method": "eth_call",
  "params": [{
    "to": "$TOKEN_CONTRACT",
    "data": "0x70a08231000000000000000000000000$TOKEN_HOLDER"
  }, "latest"],
  "id": 1
}
EOM
)

echo "$PAYLOAD"

# Make the API request
RESULT=$(curl -X POST "$NODE_URL" \
     -H "Content-Type: application/json" \
     -d "$PAYLOAD")

# Extract the balance from the result
BALANCE_HEX=$(echo "$RESULT" | jq -r '.result')
echo $BALANCE_HEX

# Convert the hex balance to decimal and format it
if [ "$BALANCE_HEX" != "0x0" ]; then
    # Convert hex to decimal using printf
    BALANCE_DECIMAL=$(printf "%d" "$BALANCE_HEX")
    # Divide by 10^18 to get the balance in DAI
    BALANCE_DECIMAL=$(echo "scale=18; $BALANCE_DECIMAL / 1000000000000000000" | bc)
else
    BALANCE_DECIMAL=0
fi

echo "Balance: $BALANCE_DECIMAL DAI"



