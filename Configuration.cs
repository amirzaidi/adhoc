using AdHocMAC.Nodes;
using AdHocMAC.Nodes.MAC;
using AdHocMAC.Nodes.MAC.Backoff;
using AdHocMAC.Utility;
using System;

namespace AdHocMAC
{
    class Configuration
    {
        public enum MACProtocol
        {
            Aloha,
            CSMANPP,
            CSMAPP,
        }

        public const MACProtocol MAC = MACProtocol.CSMAPP;
        public const int MinSlotDelayUpperbound = 4;
        public const int MaxSlotDelayUpperbound = 32;
        public const double PPersistency = 0.4;

        public const double SLOT_SECONDS = 0.1;
        // public const double SIFS_SECONDS = 0.05;

        public enum CABackoff
        {
            BEB,
            DIDD,
            Fib,
        }

        public const CABackoff CA_BACKOFF = CABackoff.Fib;
        public const int CA_MIN_TIMEOUT_SLOTS = 1;
        public const int CA_MAX_TIMEOUT_SLOTS = 32;

        public enum MessageChance
        {
            Uniform,
            ScaledUniform,
            Poisson,
        }

        public const MessageChance MESSAGE_CHANCE_TYPE = MessageChance.Poisson;

        public static readonly PoissonDistribution POISSON_DIST = new PoissonDistribution(3.0);
        public const double POISSON_DIST_DIV = 10.0;

        public const int NODE_WAKEUP_TIME_MS = (int)(0.5 * SLOT_SECONDS * 1000); // Half a slot.
        public const double NODE_CHANCE_GEN_MSG = 0.005;

        public static double CreateMessageChance(double RandDouble)
        {
            switch (MESSAGE_CHANCE_TYPE)
            {
                case MessageChance.Uniform:
                    return NODE_CHANCE_GEN_MSG;
                case MessageChance.ScaledUniform:
                    return NODE_CHANCE_GEN_MSG * RandDouble;
                case MessageChance.Poisson:
                    return POISSON_DIST.GetXForCumulativeProb(RandDouble) / POISSON_DIST_DIV;
            }
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
        }
    }
}
