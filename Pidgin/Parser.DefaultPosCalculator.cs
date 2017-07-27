using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Pidgin
{
    public static partial class Parser
    {
        private static object _charDefaultLock = new object();
        private static readonly Func<char, SourcePos, SourcePos> _charDefault =
            (t, p) => t == '\n'
                ? p.NewLine()
                : t == '\t'
                    ? new SourcePos(p.Line, p.Col + 4)
                    : p.IncrementCol();
        internal static Func<char, SourcePos, SourcePos> DefaultCharPosCalculator { get; private set; } = _charDefault;
            
        private static object _byteDefaultLock = new object();
        private static readonly Func<byte, SourcePos, SourcePos> _byteDefault = MakeDefault<byte>();
        internal static Func<byte, SourcePos, SourcePos> DefaultBytePosCalculator { get; private set; } = _byteDefault;
            
        
        private static readonly ConcurrentDictionary<Type, Delegate> _defaults =
            new ConcurrentDictionary<Type, Delegate>(new KeyValuePair<Type, Delegate>[]
            {
                new KeyValuePair<Type, Delegate>(typeof(char), _charDefault),
                new KeyValuePair<Type, Delegate>(typeof(byte), _byteDefault),
            });

        internal static Func<TToken, SourcePos, SourcePos> GetDefaultPosCalculator<TToken>()
        {
            return (Func<TToken, SourcePos, SourcePos>)_defaults.GetOrAdd(typeof(TToken), MakeDefault<TToken>());
        }

        private static Func<TToken, SourcePos, SourcePos> MakeDefault<TToken>()
            => (t, p) => p.IncrementCol();

        /// <summary>
        /// Set the default position calculator for tokens of type <typeparamref name="TToken"/>
        /// </summary>
        /// <typeparam name="TToken">The type of tokens for which to set the default position calculator</typeparam>
        /// <param name="posCalculator">A function which calculates the position after consuming a token</param>
        public static void SetDefaultPosCalculator<TToken>(Func<TToken, SourcePos, SourcePos> posCalculator)
        {
            if (posCalculator == null)
            {
                throw new ArgumentNullException(nameof(posCalculator));
            }

            var ttoken = typeof(TToken);
            if (ttoken.Equals(typeof(char)))
            {
                lock(_charDefaultLock)
                {
                    DefaultCharPosCalculator = (Func<char, SourcePos, SourcePos>)(Delegate)posCalculator;
                    _defaults[ttoken] = posCalculator;
                }
                return;
            }
            if (ttoken.Equals(typeof(byte)))
            {
                lock(_byteDefaultLock)
                {
                    DefaultBytePosCalculator = (Func<byte, SourcePos, SourcePos>)(Delegate)posCalculator;
                    _defaults[ttoken] = posCalculator;
                }
                return;
            }
            _defaults[ttoken] = posCalculator;
        }

        /// <summary>
        /// Resets the default position caluclator for tokens of type <typeparamref name="TToken"/>
        /// </summary>
        /// <typeparam name="TToken">The type of tokens for which to reset the default position calculator</typeparam>
        public static void ResetDefaultPosCalculator<TToken>()
        {
            var ttoken = typeof(TToken);
            if (ttoken.Equals(typeof(char)))
            {
                SetDefaultPosCalculator(_charDefault);
            }
            if (ttoken.Equals(typeof(byte)))
            {
                SetDefaultPosCalculator(_byteDefault);
            }
            SetDefaultPosCalculator(MakeDefault<TToken>());
        }
    }
}