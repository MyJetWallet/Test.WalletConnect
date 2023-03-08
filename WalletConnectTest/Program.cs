// See https://aka.ms/new-console-template for more information

using System.Numerics;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.Signer;
using Nethereum.Web3;
using Newtonsoft.Json;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Core.Models.Pairing;
using WalletConnectSharp.Core.Models.Pairing.Methods;
using WalletConnectSharp.Desktop;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign;
using WalletConnectSharp.Sign.Controllers;
using WalletConnectSharp.Sign.Models;

Console.WriteLine("Hello, World!");

var options = new SignClientOptions()
{
    ProjectId = "*",
    Metadata = new Metadata()
    {
        Description = "An example project to showcase WalletConnectSharpv2",
        Icons = new[] { "https://walletconnect.com/meta/favicon.ico" },
        Name = "simple-timelock-web-uat",
        Url = "https://walletconnect.com"
    },
};

var client = await WalletConnectSignClient.Init(options);


while (true)
{
    var address = "0xD8970629b60eDDE6766A4a8C74667307d7044eB2";
    var privateKey = "enter key here";
    //var stringAddress;
    Console.WriteLine($"EthECKey: {address}");

    Console.Write("QR_Code: ");
    var qrCode = Console.ReadLine();
    var proposalStruct = await client.Pair(qrCode).ConfigureAwait(false);
    Console.WriteLine($"proposalStruct: {Newtonsoft.Json.JsonConvert.SerializeObject(proposalStruct)}");

    var approveData = await client.Approve(proposalStruct, new[] { address });

    Console.WriteLine($"approveData: {Newtonsoft.Json.JsonConvert.SerializeObject(approveData)}");
    await approveData.Acknowledged();

    client.Engine.SessionRequestEvents<EthSignTransactionRequestWrapper, EthSignTransactionResponse>()
        .OnRequest += async (requestData) =>
    {
        var request = requestData.Request;
        var data = request.Params[0];

        //var transaction = new Nethereum.Model.LegacyTransaction(data.To, BigInteger.Parse(data.Value),
        //    BigInteger.Parse(data.Nonce));
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(data));
        var signer = new LegacyTransactionSigner();
        var tx = signer.SignTransaction(privateKey, Chain.Goerli, data.To, new HexBigInteger(data.Value),
            new HexBigInteger(data.Nonce), new HexBigInteger(data.GasPrice), new HexBigInteger(data.GasLimit), data.Data).EnsureHexPrefix();
        ;
        //var web3 = new Web3("https://polygon-mumbai.g.alchemy.com/v2/-H90c6yQzn4CPq18waI3UtyZfd4Bhs2m");
        //var hash = await web3.Eth.Transactions.s.SendRequestAsync(parameters[0] as string);

        Console.WriteLine(tx);

        requestData.Response = tx;
    };

    //client.Events.ListenFor();
    //client.Respond<>();

    while (true)
    {
        await Task.Delay(2000);
    }

    await client.Disconnect(proposalStruct.PairingTopic, new PairingDelete());
}

public class Approver
{
    private readonly WalletConnectSignClient _signClient;
    private readonly string _topic;

    public Approver(WalletConnectSignClient signClient, string topic)
    {
        _signClient = signClient;
        _topic = topic;
    }
    public async Task Approve(string address, string method, object[] parameters)
    {
        switch (method)
        {
            case EIP155_SIGNING_METHODS.PERSONAL_SIGN:
            case EIP155_SIGNING_METHODS.ETH_SIGN:
                break;
            //const message = getSignParamsMessage(request.params)
            //const signedMessage = await wallet.signMessage(message)
            //return formatJsonRpcResult(id, signedMessage)

            case EIP155_SIGNING_METHODS.ETH_SIGN_TYPED_DATA:
            case EIP155_SIGNING_METHODS.ETH_SIGN_TYPED_DATA_V3:
            case EIP155_SIGNING_METHODS.ETH_SIGN_TYPED_DATA_V4:
                break;
            //const { domain, types, message: data } = getSignTypedDataParamsData(request.params)
            //// https://github.com/ethers-io/ethers.js/issues/687#issuecomment-714069471
            //delete types.EIP712Domain
            //const signedData = await wallet._signTypedData(domain, types, data)
            //return formatJsonRpcResult(id, signedData)

            case EIP155_SIGNING_METHODS.ETH_SEND_TRANSACTION:


            //return formatJsonRpcResult(id, hash)

            case EIP155_SIGNING_METHODS.ETH_SIGN_TRANSACTION:
                break;
            //const signTransaction = request.params[0]
            //const signature = await wallet.signTransaction(signTransaction)
            //return formatJsonRpcResult(id, signature)

            default:
                throw new Exception("");
        }
    }
}

public class ConvertToStr<T> : JsonConverter where T : class
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string) || objectType == typeof(T);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // Deserialize the value as usual
        return serializer.Deserialize<string>(reader) as T;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.ToString());
    }
}

[RpcMethod("eth_signTransaction"), RpcRequestOptions(Clock.ONE_MINUTE, false, 99999)]
public class EthSignTransactionRequestWrapper : List<EthSignTransactionRequest>
{
}

public class EthSignTransactionRequest
{
    [JsonProperty("from")]
    public string From { get; set; }

    [JsonProperty("to")]
    public string To { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; }

    [JsonProperty("gasLimit")]
    public string GasLimit { get; set; }

    [JsonProperty("gasPrice")]
    public string GasPrice { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }

    [JsonProperty("nonce")]
    public string Nonce { get; set; }

}

[RpcResponseOptions(Clock.ONE_MINUTE, false, 99999)]
[JsonConverter(typeof(ConvertToStr<EthSignTransactionResponse>))]
public class EthSignTransactionResponse
{
    private readonly string _response;

    public EthSignTransactionResponse(string response)
    {
        _response = response;
    }

    public static implicit operator EthSignTransactionResponse(string response)
    {
        if (response == null)
            return null;

        return new EthSignTransactionResponse(response);
    }

    public override string ToString()
    {
        return _response;
    }
}

public static class EIP155_SIGNING_METHODS
{
    public const string PERSONAL_SIGN = "personal_sign";
    public const string ETH_SIGN = "eth_sign";
    public const string ETH_SIGN_TRANSACTION = "eth_signTransaction";
    public const string ETH_SIGN_TYPED_DATA = "eth_signTypedData";
    public const string ETH_SIGN_TYPED_DATA_V3 = "eth_signTypedData_v3";
    public const string ETH_SIGN_TYPED_DATA_V4 = "eth_signTypedData_v4";
    public const string ETH_SEND_RAW_TRANSACTION = "eth_sendRawTransaction";
    public const string ETH_SEND_TRANSACTION = "eth_sendTransaction";
}
