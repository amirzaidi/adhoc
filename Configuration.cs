﻿using AdHocMAC.Nodes;
using AdHocMAC.Nodes.MAC;
using AdHocMAC.Nodes.MAC.Backoff;
using System;

namespace AdHocMAC
{
    class Configuration
    {
        public enum MACProtocol
        {
            Aloha,
            UnslottedCSMANPP,
            SlottedCSMAPP,
        }

        public const MACProtocol MAC = MACProtocol.SlottedCSMAPP;
        public const int MinSlotDelayUpperbound = 4;
        public const int MaxSlotDelayUpperbound = 32;
        public const double PPersistency = 0.4;

        public const double SLOT_SECONDS = 0.1;
        // public const double SIFS_SECONDS = 0.05;

        public enum CABackoff
        {
            BEB,
            DIDD,
            Fib
        }

        public const CABackoff CA_BACKOFF = CABackoff.Fib;
        public const int CA_MIN_TIMEOUT_SLOTS = 1;
        public const int CA_MAX_TIMEOUT_SLOTS = 32;

        public const int NODE_WAKEUP_TIME_MS = (int)(0.5 * SLOT_SECONDS * 1000); // Half a slot.
        public const double NODE_CHANCE_GEN_MSG = 0.001;

        public static IMACProtocol<Packet> CreateMACProtocol(int Seed)
        {
            switch (MAC)
            {
                case MACProtocol.Aloha:
                    return new Aloha();
                case MACProtocol.UnslottedCSMANPP:
                    return new CarrierSensingNonPersistent(new Random(Seed));
                case MACProtocol.SlottedCSMAPP:
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