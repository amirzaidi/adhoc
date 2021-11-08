using AdHocMAC.Nodes;
using AdHocMAC.Nodes.MAC;
using AdHocMAC.Nodes.MAC.Backoff;
using AdHocMAC.Utility;
using System;

namespace AdHocMAC
{
    class Configuration
    {
        public static int AUTO_RUN_SHUT_DOWN_AFTER = -1;
        public static bool AUTO_RUN_PACKETS_ENABLED = false;
        public static int AUTO_RUN_NODE_COUNT = 2;
        public static bool AUTO_RUN_FULLY_CONNECTED = false;
        public static double AUTO_RUN_TRAFFIC = 0.1; // This should be a number in [0,1], used both for Poisson and uniform traffic
        public static double AUTO_RUN_POISSON_PARAMETER = 5;

        public enum MACProtocol
        {
            Aloha = 0,
            CSMANPP = 1,
            CSMAPP = 2,
        }
    
        public static MACProtocol MAC = MACProtocol.CSMAPP;
        public const int MinSlotDelayUpperbound = 4;
        public const int MaxSlotDelayUpperbound = 32;

        public static double PPersistency = 1.0; // default for 802.11 DCF standard.

        public const double SLOT_SECONDS = 0.1;
        public const double SIFS_SECONDS = 0.3;

        public enum CABackoff
        {
            BEB = 'B',
            DIDD = 'D',
            Fib = 'F',
        }
        
        public static CABackoff CA_BACKOFF = CABackoff.BEB;
        public const int CA_MIN_TIMEOUT_SLOTS = 1;
        public const int CA_MAX_TIMEOUT_SLOTS = 32;

        public enum MessageChance
        {
            Uniform = 'u',
            ScaledUniform = 's',
            Poisson = 'p',
        }

        public static MessageChance MESSAGE_CHANCE_TYPE = MessageChance.Uniform;
        public static readonly PoissonDistribution POISSON_DIST = new PoissonDistribution(AUTO_RUN_POISSON_PARAMETER);

        public const int NODE_WAKEUP_TIME_MS = (int)(0.5 * SLOT_SECONDS * 1000); // Half a slot.
        public const int NODE_PACKET_RETRY_ATTEMPTS = 16;

        public static double CreateMessageChance(double RandDouble)
        {
            switch (MESSAGE_CHANCE_TYPE)
            {
                case MessageChance.Uniform:
                    return AUTO_RUN_TRAFFIC;
                case MessageChance.ScaledUniform:
                    return AUTO_RUN_TRAFFIC * RandDouble;
                case MessageChance.Poisson:
                    return POISSON_DIST.GetXForCumulativeProb(RandDouble) * AUTO_RUN_TRAFFIC;
            }
            return 0.0;
        }

        public const double PHYSICS_RANGE = 80.0; // for path set range 80.0
        public const double TRANSMISSION_CHAR_PER_SECOND = 256.0; // Characters sent per second.
        public const double TRANSMISSION_DIST_PER_SECOND = 2048.0; // Speed of light in this system.

        public static IMACProtocol<Packet> CreateMACProtocol(int Seed)
        {
            switch (MAC)
            {
                case MACProtocol.Aloha:
                    return new Aloha();
                case MACProtocol.CSMANPP:
                    return new CarrierSensingNonPersistent(new Random(Seed));
                case MACProtocol.CSMAPP:
                    return new CarrierSensingPPersistent(new Random(Seed));
            }
            return null;
        }

        public static IBackoff CreateBackoff()
        {
            switch (CA_BACKOFF)
            {
                case CABackoff.BEB:
                    return new BinaryExponential(CA_MIN_TIMEOUT_SLOTS, CA_MAX_TIMEOUT_SLOTS);
                case CABackoff.DIDD:
                    return new DIDD(CA_MIN_TIMEOUT_SLOTS, CA_MAX_TIMEOUT_SLOTS);
                case CABackoff.Fib:
                    return new Fibonacci(CA_MAX_TIMEOUT_SLOTS);
            }
            return null;
        }
    }
}
