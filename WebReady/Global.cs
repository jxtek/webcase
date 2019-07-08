﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using WebReady.Db;

namespace WebReady.Web
{
    /// <summary>
    /// The application scope that holds global states.
    /// </summary>
    public class Global
    {
        public const string APP_JSON = "app.json";

        public const string CERT_PFX = "cert.pfx";

        internal static readonly WebLifetime Lifetime = new WebLifetime();

        public static readonly AppJson Config;

        public static readonly JObj Json;


        static readonly Map<string, WebService> services = new Map<string, WebService>(4);

        static readonly Map<string, WebUse> references = new Map<string, WebUse>(16);

        static readonly Map<string, DbSource> sources = new Map<string, DbSource>(4);


        internal static readonly string Sign;

        static readonly GlobalLogger Logger;

        // configured connectors that connect to peer services
        static readonly Map<string, WebUse> Ref;

        static List<WebUse> polls;

        // the thread schedules and drives periodic jobs, such as event polling 
        static Thread scheduler;

        public static readonly WebService WebService;

        static Global()
        {
            // setup logger
            string logfile = DateTime.Now.ToString("yyyyMM") + ".log";
            Logger = new GlobalLogger(logfile);
            if (!File.Exists(APP_JSON))
            {
                Logger.Log(LogLevel.Error, APP_JSON + " not found");
                return;
            }

            // load the configuration file
            byte[] bytes = File.ReadAllBytes(APP_JSON);
            JsonParser parser = new JsonParser(bytes, bytes.Length);
            Json = (JObj) parser.Parse();
            Config = new AppJson();
            Config.Read(Json, 0xff);

            if (Config.logging > 0)
            {
                Logger.Level = Config.logging;
            }

            // references
            var r = Config.@ref;
            if (r != null)
            {
                for (int i = 0; i < r.Count; i++)
                {
                    var e = r.EntryAt(i);
                    if (Ref == null)
                    {
                        Ref = new Map<string, WebUse>(16);
                    }

                    Ref.Add(new WebUse(e.Key, e.Value)
                    {
                        Clustered = true
                    });
                }
            }

            Sign = Encrypt(Config.cipher.ToString());


            // create and start the scheduler thead
            if (polls != null)
            {
                // to repeatedly check and initiate event polling activities.
                scheduler = new Thread(() =>
                {
                    while (true)
                    {
                        // interval
                        Thread.Sleep(1000);

                        // a schedule cycle
                        int tick = Environment.TickCount;
                        for (int i = 0; i < polls.Count; i++)
                        {
                            var cli = polls[i];
                            cli.TryPollAsync(tick);
                        }
                    }
                });
                scheduler.Start();
            }
        }

        public void AddService<T>(string key) where T : WebService, new()
        {
        }


        // LOGGING

        public static void TRC(string msg, Exception ex = null)
        {
            if (msg != null)
            {
                Logger.Log(LogLevel.Trace, 0, msg, ex, null);
            }
        }

        public static void DBG(string msg, Exception ex = null)
        {
            if (msg != null)
            {
                Logger.Log(LogLevel.Debug, 0, msg, ex, null);
            }
        }

        public static void INF(string msg, Exception ex = null)
        {
            if (msg != null)
            {
                Logger.Log(LogLevel.Information, 0, msg, ex, null);
            }
        }

        public static void WAR(string msg, Exception ex = null)
        {
            if (msg != null)
            {
                Logger.Log(LogLevel.Warning, 0, msg, ex, null);
            }
        }

        public static void ERR(string msg, Exception ex = null)
        {
            if (msg != null)
            {
                Logger.Log(LogLevel.Error, 0, msg, ex, null);
            }
        }


        public static void Schedule(string refName, Action<IPollContext> poller, short interval = 12)
        {
            if (Ref == null)
            {
                throw new WebException("missing ref in config.json");
            }

            // setup context for each designated client
            int match = 0;
            for (int i = 0; i < Ref.Count; i++)
            {
                var @ref = Ref.ValueAt(i);
                if (@ref.Key == refName)
                {
                    @ref.SetPoller(poller, interval);
                    if (polls == null) polls = new List<WebUse>();
                    polls.Add(@ref);
                    match++;
                }
            }

            if (match == 0)
            {
                throw new WebException("webconfig refs missing " + refName);
            }
        }


        public static DbContext NewDbContext(IsolationLevel? level = null)
        {
            DbContext dc = new DbContext(Config);
            if (level != null)
            {
                dc.Begin(level.Value);
            }

            return dc;
        }

        static readonly CancellationTokenSource Cts = new CancellationTokenSource();

        /// 
        /// Runs a number of web services and block until shutdown.
        /// 
        public static void StartWebServer()
        {
            var exit = new ManualResetEventSlim(false);


            // start service instances
            WebService.StartAsync(Cts.Token).GetAwaiter().GetResult();

            // handle SIGTERM and CTRL_C 
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                Cts.Cancel(false);
                exit.Set(); // release the Main thread
            };
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Cts.Cancel(false);
                exit.Set(); // release the Main thread
                // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                eventArgs.Cancel = true;
            };
            Console.WriteLine("CTRL + C to shut down");

            Lifetime.NotifyStarted();

            // wait on the reset event
            exit.Wait(Cts.Token);

            Lifetime.StopApplication();

            WebService.StopAsync(Cts.Token).GetAwaiter().GetResult();

            Lifetime.NotifyStopped();
        }

        public static X509Certificate2 BuildSelfSignedCertificate(string dns, string ipaddr, string issuer, string password)
        {
            SubjectAlternativeNameBuilder sanb = new SubjectAlternativeNameBuilder();
            sanb.AddIpAddress(IPAddress.Parse(ipaddr));
            sanb.AddDnsName(dns);

            X500DistinguishedName subject = new X500DistinguishedName($"CN={issuer}");

            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(new OidCollection {new Oid("1.3.6.1.5.5.7.3.1")}, false));

                request.CertificateExtensions.Add(sanb.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
                certificate.FriendlyName = issuer;

                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, password), password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
            }
        }

        //
        // encrypt / decrypt
        //

        public static string Encrypt(string v)
        {
            byte[] bytebuf = Encoding.ASCII.GetBytes(v);
            int count = bytebuf.Length;
            int mask = Config.cipher;
            int[] masks = {(mask >> 24) & 0xff, (mask >> 16) & 0xff, (mask >> 8) & 0xff, mask & 0xff};
            char[] charbuf = new char[count * 2]; // the target
            int p = 0;
            for (int i = 0; i < count; i++)
            {
                // masking
                int b = bytebuf[i] ^ masks[i % 4];

                //transform
                charbuf[p++] = HEX[(b >> 4) & 0x0f];
                charbuf[p++] = HEX[(b) & 0x0f];

                // reordering
            }

            return new string(charbuf, 0, charbuf.Length);
        }

        public static string Encrypt<P>(P prin, byte proj) where P : IData
        {
            JsonContent cnt = new JsonContent(true, 4096);
            try
            {
                cnt.Put(null, prin, proj);
                byte[] bytebuf = cnt.ByteBuffer;
                int count = cnt.Size;

                int mask = Config.cipher;
                int[] masks = {(mask >> 24) & 0xff, (mask >> 16) & 0xff, (mask >> 8) & 0xff, mask & 0xff};
                char[] charbuf = new char[count * 2]; // the target
                int p = 0;
                for (int i = 0; i < count; i++)
                {
                    // masking
                    int b = bytebuf[i] ^ masks[i % 4];

                    //transform
                    charbuf[p++] = HEX[(b >> 4) & 0x0f];
                    charbuf[p++] = HEX[(b) & 0x0f];

                    // reordering
                }

                return new string(charbuf, 0, charbuf.Length);
            }
            finally
            {
                // return pool
                BufferUtility.Return(cnt);
            }
        }

        public static P Decrypt<P>(string token) where P : IData, new()
        {
            int mask = Config.cipher;
            int[] masks = {(mask >> 24) & 0xff, (mask >> 16) & 0xff, (mask >> 8) & 0xff, mask & 0xff};
            int len = token.Length / 2;
            var str = new Text(1024);
            int p = 0;
            for (int i = 0; i < len; i++)
            {
                // TODO reordering

                // transform to byte
                int b = (byte) (Dv(token[p++]) << 4 | Dv(token[p++]));
                // masking
                str.Accept((byte) (b ^ masks[i % 4]));
            }

            // deserialize
            try
            {
                JObj jo = (JObj) new JsonParser(str.ToString()).Parse();
                P prin = new P();
                prin.Read(jo, 0xff);
                return prin;
            }
            catch
            {
                return default;
            }
        }

        public static string Decrypt(string v)
        {
            int mask = Config.cipher;
            int[] masks = {(mask >> 24) & 0xff, (mask >> 16) & 0xff, (mask >> 8) & 0xff, mask & 0xff};
            int len = v.Length / 2;
            var str = new Text(1024);
            int p = 0;
            for (int i = 0; i < len; i++)
            {
                // TODO reordering

                // transform to byte
                int b = (byte) (Dv(v[p++]) << 4 | Dv(v[p++]));
                // masking
                str.Accept((byte) (b ^ masks[i % 4]));
            }

            return str.ToString();
        }

        // hexidecimal characters
        static readonly char[] HEX = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

        // return digit value
        static int Dv(char hex)
        {
            int v = hex - '0';
            if (v >= 0 && v <= 9)
            {
                return v;
            }

            v = hex - 'A';
            if (v >= 0 && v <= 5) return 10 + v;
            return 0;
        }
    }
}