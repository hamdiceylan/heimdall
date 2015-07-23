<img align="left" src="https://avatars0.githubusercontent.com/u/7360948?v=3" />

&nbsp;Heimdall<br /><br />
=============

| Downloads | Version |
|-----------|---------|
| ![NuGet Total](https://img.shields.io/nuget/dt/Heimdall.svg) | ![NuGet Version](https://img.shields.io/nuget/v/Heimdall.svg) |

Easy to use HMAC Digest Authentication for WebAPI with various client implementations (C#, NodeJS and Browser versions)

##How it works

Let's first take a look at the component parts. Heimdall is broken up into two distinct logical parts, namely, `server` and `client`. 
Both create compatible signed messages using the the individual message representation for each request using certain key dimensions. 
So for example if we were to make a get request like the one below:

**Insecure GET Request**

A simple vanilla request with no authentication.

*Headers*
    
    Accept: */*
    Content-Type: application/json
    
*Path*
    
    GET /api/mysecureresource/1
  
You would require a `username` and a `secret` to sign the message, this is already implemented for you in a Heimdall C# and JavaScript
client which we will cover in more detail later on. Once the message is sent to the server a delegating handler will then verify the 
message and then decide whether it is valid or not. So if our username was 'username' and the secret was 'secret' then our example
request would now look something like this: 

**Heimdall GET Request**

A request generated by a Heimdall client.

*Headers*
    
    Accept: */*
    Content-Type: application/json
    X-ApiAuth-Username: username
    X-ApiAuth-Date: Thu, 23 Jul 2015 10:48:42 GMT
    Authorization: ApiAuth CLcQbLlK3HajC/PPpwwxLoqHCnCrlM1VBjN8TGnYjuM=
    
*Path*
    
    GET /api/mysecureresource/1
  
Here you can see how Heimdall calculates an authorisation hash using the message representation and adds additional headers so 
that the server can identify the user in order to rebuild the authorisation hash on the server. If these hashes match, then 
the request is allowed through, if not then a http response code 401 will be returned.

##Clients

There are 3 versions of clients that can be found in the `examples` folder of the source code for Heimdall. Let's take a look at
each one starting with the C# client first. 

###The C# Client

This client comes in two flavours. One that is Castle Windsor ready(for use with FluentWindsor) or one without. Use the latter if 
you are not using Castle Windsor as your IoC container. 

####Heimdall.Client

This is the non windsor version. Start by installing the `Heimdall.Client` NuGet. Once this is done let's look at how
you would make a simple signed get request using a HttpClient. 

```csharp
HttpClient client = HeimdallClientFactory.Create("myusername", "mysecret");
var content = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("firstName", "Alex"),
    new KeyValuePair<string, string>("lastName", "Brown")
});
var result = client.PostAsync("http://requestb.in/14nmm871", content).Result;
```

If you would like to see a working example of this, please see the console application .\examples\Example.Client after
opening the solution in Visual Studio. 

####Heimdal.Client.Windsor

Let's look at how we initialise a client using FluentWindsor. First start by installing the `Heimdall.Client.Windsor` NuGet. 
Next you would have to make sure that FluentWindsor is initialised(this should only be called once on startup).

```csharp
FluentWindsor.NewContainer(typeof(Program).Assembly).WithArrayResolver().WithInstallers().Create();
```

This will automatically pick up the `IWindsorInstaller` and install an instance of the `IHeimdallClientFactory` which can 
then be injected for consumption via any constructor known to the container. For demonstration purposes we are going to use
the ServiceLocator of FluentWindsor.

```csharp
var client = FluentWindsor.ServiceLocator.Resolve<IHeimdallClientFactory>().Create("username", "secret");
var content = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("firstName", "Alex"),
    new KeyValuePair<string, string>("lastName", "Brown")
});

var result = client.PostAsync("http://requestb.in/14nmm871", content).Result;
```

If you would like to see a working example of this, please see the console application .\examples\Example.Client.FluentWindsor 
after opening the solution in Visual Studio. 

###The NodeJs Client

A proper client has not yet been published to NPM but we have plans to do this in the near future. This is an example of how you would roll a 
`Heimdall` request using NodeJs. 

####Dependencies

First start by installing the REST client 'request' like so:

    npm install request
    
Once this is done, create a file called `app.js` and place the following code at the top of the file. These are the two libraries we would require,
one for doing requests and one for doing encryption so we can hash message representations.

```javascript
var request = require('request');
var crypto = require('crypto');
```

####Encryption

Next you are going to need a little utility function that can do HMAC encryption using SHA256 with base64 encoding like so:

```javascript
function encrypt(data, secret) {
    var hmacSignature = crypto.createHmac("sha256", secret || '');
    hmacSignature.update(messageRepresentation);
    return hmacSignature.digest("base64");
}
```

####Message Representation

Next lets set about the task of building up a message representation. A message representation is logically comprised of the following elements: 

```
HTTP Method ['GET','POST','PUT','DELETE']
HTTP Path [Example: '/api/values']
Content-Type Header [Example: 'application/json']
Content-MD5 Header [Content Checksum, applicable to 'POST','PUT']
Timstamp: [Example: Thu, 23 Jul 2015 13:04:27 GMT]
```

So now let's go ahead and create our message representation in JavaScript like so:

```javascript
var httpMethod = 'GET';
var httpPath = '/api/values';
var contentType = 'application/json';
var contentMD5 = '';
var timestamp = new Date().toUTCString();
var messageRepresentation = [httpMethod, httpPath, contentType, contentMD5, timestamp].join('\n');
```

You will notice that the contentMD5 portion is empty. This is intentional as GET requests do not have content in their body. A 
POST or PUT however does generally have a body. You can easily setup a hash using the encrypt function without a secret like
so:

```javascript
var body = { any:'value' };
var contentMD5 = encrypt(JSON.stringify(body));
```

####Wrapping it up

Next let's build our request object and finally make the request to our Heimdall example server, pay special attention to what 
is going on with the headers:

```javascript
var req = {
    url: 'http://localhost:12345/api/values',
    headers: {
        'X-ApiAuth-Date': timestamp,
        'X-ApiAuth-Username': 'username',
        'Content-MD5': contentMD5,
        'Content-Type': 'application/json',
        'Authorization': 'ApiAuth ' + encrypt(messageRepresentation, 'secret')
    }
};

console.log('Request: ');
console.log(req);
console.log();

request(req, function (error, response, body) {
    if (!error) {
        console.log("Response:");
        console.log(response.statusCode);
        console.log(response.body);
    } else {
        console.log(error);
    }
});
```

Download the source and start up the Example.IIS project followed by running the NodeJs example at the following location:

[https://github.com/cryosharp/heimdall/blob/master/Example.Client.NodeJs/app.js](https://github.com/cryosharp/heimdall/blob/master/Example.Client.NodeJs/app.js)
      
###The Pure Js Client

Here is another example of a Heimdall client that runs in a browser. Very useful if you are doing web development. You will need a few
helper libraries to achieve this namely `crypto-js` and `jquery`. You can get both using bower, not sure if crypto-js is available
on NuGet. 

####Dependencies

If you have NodeJs installed then run the following commands: 

    npm install bower -g
    bower install jquery
    bower install crypto-js
    
The bower packages should be installed in a folder called `bower_components` in the directory from which you ran the console commands 
above. 

####Script references

Next you would need to put the following script references somewhere in your document: 

```html
<script src="~/Scripts/bower_components/jquery/dist/jquery.min.js"></script>
<script src="~/Scripts/bower_components/crypto-js/crypto-js.js"></script>
```

Make sure that the relative path matches to the location from where you installed the bower packages. Next you will need to manually
embed the browser based Heimdall JS client. You can do this by copying the file from the link below: 

[https://github.com/cryosharp/heimdall/blob/master/Example.IIS/Scripts/heimdall.js](https://github.com/cryosharp/heimdall/blob/master/Example.IIS/Scripts/heimdall.js)

You can also embed this using a script tag like the following: 

```html
<script src="~/Scripts/heimdall.js"></script>
```

####Example requests

Make sure this script reference is placed after the jquery and crypto script reference or else it wont work. Once complete you are then 
free to start rolling Heimdall browser based requests using the following javascript: 

```javascript
//Example GET
var _h = new Heimdall('http://localhost:12345', 'username', 'secret');

_h.get('/api/values', function (err, res) {
    if (!err) {
        alert('GET Success!');
    } else {
        alert('GET Error!');
    }
});
```

Similarly if you were doing a POST or PUT request where you have a body then you would do something like so:

```javascript
//Example GET
var _h = new Heimdall('http://localhost:12345', 'username', 'secret');
_h.post('/api/values', 'hello world', function(err, res) {
    if (!err) {
        alert('POST Success!');
    } else {
        alert('POST Error!');
    }
});
```

There are also plans to make this client available on bower in the near future.

##Problems?

For any problems please sign into github and raise issues. 

