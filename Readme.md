TeleBot.AwsLambdaAOT allows you to send videos to a Telegram chat using Instagram or TikTok URLs
```
dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
dotnet tool update -g Amazon.Lambda.Tools
```

Create a Telegram bot and paste the token into appsettings.json using ```@BotFather```.

Authorize in AWS CLI ```aws configure``` and enter credentials. (Credentials stores in `%UserProfile%/.aws`).

Deploy the function to AWS Lambda:
```
cd "TeleBot.AwsLambdaAOT"
```
```
dotnet lambda deploy-function --name TeleLambdaAOT
```

