#include <SPI.h>
#include <ESP8266WiFi.h>
#include <ESP8266WiFiMulti.h>
//#include <WiFiUdp.h>
#include <ESP8266WiFiType.h>
//#include <ESP8266WiFiAP.h>
#include <WiFiClient.h>
#include <WiFiClientSecure.h>
#include <WiFiServer.h>
//#include <ESP8266WiFiScan.h>
#include <ESP8266WiFiGeneric.h>
#include <ESP8266WiFiSTA.h>
#include <ESP8266HTTPClient.h>
#include <WiFiClientSecureBearSSL.h>
#include <ArduinoJson.h>
#include <ArduinoOTA.h>

/*****************************************/
const int led = LED_BUILTIN; //13;//the led attach to
const int pin = D2; // NodeMCU uses internal pin numbers!

// char ssid[] = "Keenetic-7548";  //  your network SSID (name)
// char pass[] = "UuP4EJ8L";  // your network password
char ssid2[] = "Keenetic-1738";  //  your network 2 SSID (name)
char pass2[] = "FX4vACfS";  // your network 2 password

int wifiStatus = WL_IDLE_STATUS;     // the Wifi radio's status

// // Variables will change:
// int ledState = HIGH;      // the current state of the output pin
// int pinState;             // the current reading from the input pin
// int lastPinState = LOW;   // the previous reading from the input pin

String host = "gigavat.keenetic.pro"; //"192.168.6.114"; //
int port = 2884; // 8080; //30080; 
//int httpsPort = 30443;       
String url;

String apiKeyHeader = "X-API-Key";
String apiKeyValue = "1234567890";

//const long interval = 60*60*1000; // 1 hour - for "production"
// const long interval = 10*1000; // 10 seconds - for testing!
const long interval = 60*1000; 
const long intervalWhenFail = 10*1000;

const long healthCheckInterval = 60*60*1000;

// volatile long lowDebounce = 0;
// volatile long highDebounce = 0;
// const long debounceTimeout = 5*1000; // 5 sec

// WiFi connect timeout per AP. Increase when connecting takes longer.
const uint32_t connectTimeoutMs = 10000;

// const char cert [] PROGMEM = R"CERT(
// -----BEGIN CERTIFICATE-----
// MIIDazCCAlOgAwIBAgIUerY4c8Myed0Mpyi7FNsA5q/SJi8wDQYJKoZIhvcNAQEL
// BQAwRTELMAkGA1UEBhMCUlUxEzARBgNVBAgMClNvbWUtU3RhdGUxITAfBgNVBAoM
// GEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZDAeFw0yNDA4MTYxOTIxMTBaFw0zNDA4
// MTQxOTIxMTBaMEUxCzAJBgNVBAYTAlJVMRMwEQYDVQQIDApTb21lLVN0YXRlMSEw
// HwYDVQQKDBhJbnRlcm5ldCBXaWRnaXRzIFB0eSBMdGQwggEiMA0GCSqGSIb3DQEB
// AQUAA4IBDwAwggEKAoIBAQDMxfytPtIwzM04cmVz3cq2Td6MPjT8w0Q5sXF8uUS4
// Q2XX+ovkawGlBJuXtqXUWFz+rKwJi8O6B68sObHDbqnGvvnVc1XDhi8j57bJ8Tsi
// MSuJhV2mkC66aXxQ1cKMNjbc0N/qQ6Kl9RGVJcwCM4/wCZRurThWeaU2R8RVqro7
// j6jPxFLg5sY7fhED/RTmz58Eiqm+sTSsX0HZ24U5dZDYitECqfbO9IY6Ws5hsO4M
// HHYMI62dgxl9cC0zpBG7ofR55rI+sd67TddmWMAgCLwmjPKH32gle3ccgqTvWN8W
// NETsKwceqiqplE70iTWGQlqcwiofG8dNPvAn1Gd1dw3PAgMBAAGjUzBRMB0GA1Ud
// DgQWBBSrOjB5IMUOfMt852ChTAThjaQyHDAfBgNVHSMEGDAWgBSrOjB5IMUOfMt8
// 52ChTAThjaQyHDAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUAA4IBAQCy
// 1/Y1JpDWN7bgyq/mtEntOg+FL/GRJDjn8FF2pzIfD4nJ5yUKF7PhcfkmpjZxPITh
// iCbpgref7pP2uDaYX1X1kTYHr8CTvbxP8PscN8lCyT+McyKIeUTI53gEGFDZMPeE
// GaCy0QmBlXrxnke20nJ+Lm36235l27c3cNkL5SM5irxmPl+CJK/VFXNk2eLnHfZb
// uEAfXjjpCUPLnkGpbQ6taqwIZXV6SPauSmWY+EFO259argdoRzN1ttzKRYLi/mDj
// 53EGg1p42Nf+H+9WIxYmW1bj64COEP5JbaCfRGYmAjElCl3ORefmK5dug7UkBAf5
// /owhlFX900HvkVyeckrH
// -----END CERTIFICATE-----
// )CERT";


ESP8266WiFiMulti wifiMulti;

volatile long lowTime = 0;

volatile bool needPulse = false;
volatile bool canPulse = true;
volatile bool canWrite = false;
volatile long lastWriteTimeMs = 0;

volatile long pulseCount = 0;
volatile long lastReport = 0;
volatile long lastHealthCheck = 0;

const int minTimeLimitMs = 1000;

// struct MetricDto {
//   int MetricValue;
//   long WriteTimeMs;
// };

volatile long metricsIndexer = 0;
const long metricsCount = 1024;
long metrics[metricsCount];

void IRAM_ATTR onPulse() {
  long curTime = millis();
  long reading = digitalRead(pin);

  digitalWrite( led, reading );
  
  switch( reading ) {
    case LOW: // SWITCH DOWN = ON
      lowTime = curTime;
      if (canPulse)
      {
        needPulse = true;
        canPulse = false;
      }
      break;
    case HIGH: // SWITCH UP = OFF
      if (needPulse) {
        lastWriteTimeMs = curTime - lowTime;
        if (lastWriteTimeMs > minTimeLimitMs) {
          canWrite = true;
        }
        else {
          needPulse = false;
        }
      }
      break;
  }
}

void connectToWifi() {
  // WiFi.scanNetworks will return the number of networks found
  int n = WiFi.scanNetworks();
  Serial.println("scan done");
  if (n == 0) {
      Serial.println("No networks found");
  } 
  else {
    Serial.print(n);
    Serial.println(" networks found");
    for (int i = 0; i < n; ++i) {
      // Print SSID and RSSI for each network found
      Serial.print(i + 1);
      Serial.print(": ");
      Serial.print(WiFi.SSID(i));
      Serial.print(" (");
      Serial.print(WiFi.RSSI(i));
      Serial.print(")");
      Serial.println((WiFi.encryptionType(i) == AUTH_OPEN)?" ":"*");
      delay(10);
    }
  }

  Serial.println("Connecting Wifi...");
  while (wifiMulti.run() != WL_CONNECTED) {
    Serial.print(".");
    delay(500);
  }
  Serial.println("");
  Serial.println("WiFi connected");
  Serial.println("IP address: ");
  Serial.println(WiFi.localIP());
}

void setup() {
  Serial.begin(115200);
  Serial.println("Starting...");
  pinMode(led, OUTPUT);
  pinMode(pin,INPUT_PULLUP);
  digitalWrite(led, HIGH);
  attachInterrupt(digitalPinToInterrupt(pin), onPulse, CHANGE);

  Serial.print("Connecting WiFi ");
  WiFi.disconnect();
  WiFi.mode(WIFI_STA);
  // wifiMulti.addAP(ssid, pass);
  wifiMulti.addAP(ssid2, pass2);
  connectToWifi(); 

  // // Port defaults to 8266
  // ArduinoOTA.setPort(8266);
 
  // // Hostname defaults to esp8266-[ChipID]
  // ArduinoOTA.setHostname("gigavat-esp8266-[ChipID]");
 
  // No authentication by default
  ArduinoOTA.setPassword((const char *)"2wsxdr5tgbhu8");
 
  ArduinoOTA.onStart([]() {
    Serial.println("Start");
  });
  ArduinoOTA.onEnd([]() {
    Serial.println("\nEnd");
  });
  ArduinoOTA.onProgress([](unsigned int progress, unsigned int total) {
    Serial.printf("Progress: %u%%\r", (progress / (total / 100)));
  });
  ArduinoOTA.onError([](ota_error_t error) {
    Serial.printf("Error[%u]: ", error);
    if (error == OTA_AUTH_ERROR) Serial.println("Auth Failed");
    else if (error == OTA_BEGIN_ERROR) Serial.println("Begin Failed");
    else if (error == OTA_CONNECT_ERROR) Serial.println("Connect Failed");
    else if (error == OTA_RECEIVE_ERROR) Serial.println("Receive Failed");
    else if (error == OTA_END_ERROR) Serial.println("End Failed");
  });
  ArduinoOTA.begin();
  Serial.println("Ready");
}
// ======================================================================
void loop() {
  ArduinoOTA.handle();

  if (needPulse && canWrite)
  {
    Serial.println("Pulse");

    pulseCount++;
    metrics[metricsIndexer] = lastWriteTimeMs;

    metricsIndexer++;
    if (metricsIndexer == metricsCount){
      metricsIndexer = 0;
    }
    
    needPulse = false;
    canWrite = false;
    canPulse = true;
  }

  // if (needPulse && millis() - highDebounce >= debounceTimeout) {
  //   needPulse = false;
  //   pulseCount++;
  //   slog("PULSE");
  // } 

    if (wifiMulti.run(connectTimeoutMs) == WL_CONNECTED) {
      if (pulseCount > 0
          && millis()-lastReport > interval) {

        int httpCode = postHttp();
        if (httpCode >= 200 && httpCode < 300) {
          lastReport = millis();
          pulseCount = 0;
          metricsIndexer = 0;
        }
        else {
          lastReport = millis() + interval - intervalWhenFail;
        }
      }

      if (millis()-lastHealthCheck > healthCheckInterval) {
        healthChechHttp();
        lastHealthCheck = millis();
      }

    } else {
      Serial.println("WiFi not connected!");
      delay(1000);
      connectToWifi();
    }
}
// ======================================================================
// void getHttp() {
//   url = String("/apikey/2AX8Q2Y3HE5VT6DBD16N")
//     + String("/device/")+getMacAddress()
//     + String("/metric/count")
//     + String("/value/")+pulseCount;
//   slog(host + String(":") + String(port) + url);

//   WiFiClient client;

//   HTTPClient http;

//   if (http.begin(client, host, port, url)) {
//     int httpCode = http.GET();
//     if( httpCode ) {
//       if( httpCode == 200) {
//         pulseCount = 0;
//         String payload = http.getString();
//         Serial.println( payload );
//       }
//       lastReport = millis();
//     }
//     Serial.println("closing connection");
//     http.end();
//   } else {
//       Serial.println("HTTP: Unable to connect");
//   }
// }

int healthChechHttp()
{
  Serial.println("Helth check");
  
  WiFiClient client;

  HTTPClient http;

  int httpCode = -1;
  url = "/api/Health";
  if (http.begin(client, host, port, url)) {
    http.addHeader("Content-Type", "application/json; charset=utf-8");
    http.addHeader(apiKeyHeader, apiKeyValue);
    httpCode = http.GET();

    Serial.print("Url: ");
    Serial.print(host);
    Serial.print(":");
    Serial.print(port);
    Serial.println(url);
    Serial.print("HttpCode: ");
    Serial.println(httpCode);
    
    if( httpCode ) {
      if( httpCode >= 200 && httpCode < 300) {
        String payload = http.getString();
        Serial.println( payload );
      }
    }
    http.end();
  } else {
      Serial.println("HTTP: Unable to connect");
  }

  return httpCode;
}

int postHttp()
{
  StaticJsonDocument<4100> doc;
  String json;

  JsonObject root = doc.to<JsonObject>();
  root["pulseCount"] = pulseCount;

  JsonArray timings = root.createNestedArray("timings");
  for (int i = 0; i < metricsIndexer; i++)
  {
    timings.add(metrics[i]);
  }

  // StaticJsonObject allocates memory on the stack, it can be
  // replaced by DynamicJsonDocument which allocates in the heap.
  //
  // DynamicJsonDocument  doc(200);

  // doc["apikey"] = "2AX8Q2Y3HE5VT6DBD16N";
  // doc["device"] = getMacAddress();
  // doc["metric"] = "count";
  // doc["value"] = pulseCount;
  serializeJson(doc, json);

  Serial.println("Send info to server:");
  Serial.println(json);
  
  WiFiClient client;

  HTTPClient http;

  int httpCode = -1;
  url = "/api/metrics";
  if (http.begin(client, host, port, url)) {
    http.addHeader("Content-Type", "application/json; charset=utf-8");
    http.addHeader(apiKeyHeader, apiKeyValue);
    httpCode = http.POST(json);

    Serial.print("Url: ");
    Serial.print(host);
    Serial.print(":");
    Serial.print(port);
    Serial.println(url);
    Serial.print("HttpCode: ");
    Serial.println(httpCode);
    
    if( httpCode ) {
      if( httpCode >= 200 && httpCode < 300) {
        String payload = http.getString();
        Serial.println( payload );
      }
    }
    http.end();
  } else {
      Serial.println("HTTP: Unable to connect");
  }

  return httpCode;
}

// void postHttps()
// {
//   StaticJsonDocument<512> doc;
//   String json;

//   doc["apikey"] = "2AX8Q2Y3HE5VT6DBD16N";
//   doc["device"] = getMacAddress();
//   doc["metric"] = "count";
//   doc["value"] = pulseCount;
//   serializeJson(doc, json);

//   slog(json);

//   auto certs = std::make_unique<BearSSL::X509List>(cert);
//   auto client = std::make_unique<BearSSL::WiFiClientSecure>();

//   client->setTrustAnchors(certs.get());

//   HTTPClient http;

//   url = "/metrics";
//   if (http.begin(*client, host, httpsPort, url)) {
//     http.addHeader("Content-Type", "application/json; charset=utf-8");
//     int httpCode = http.POST(json);
//     if( httpCode ) {
//       if( httpCode == 200) {
//         pulseCount = 0;
//         String payload = http.getString();
//         Serial.println( payload );
//       }
//       lastReport = millis();
//     }
//     Serial.println("closing connection");
//     http.end();
//   } else {
//       Serial.println("HTTP: Unable to connect");
//   }
// }

// ======================================================================
String getMacAddress() {
  byte mac[6];
  WiFi.macAddress(mac);
  return 
     String( mac[5], HEX ) +":"
    +String( mac[4], HEX ) +":"
    +String( mac[3], HEX ) +":"
    +String( mac[2], HEX ) +":"
    +String( mac[1], HEX ) +":"
    +String( mac[0], HEX );
}
// ======================================================================