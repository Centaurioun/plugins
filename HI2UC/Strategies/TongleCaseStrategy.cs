using System.Globalization;
using System.Linq;

namespace Nikse.SubtitleEdit.PluginLogic.Strategies
{
    public class TongleCaseStrategy : ICaseStrategy
    {
        public TongleCaseStrategy(CultureInfo culture)
        {
            Culture = culture;
        }

        abstract class TogleStateAbstract
        {
            protected readonly TongleContext Context;

            public TogleStateAbstract(TongleContext context)
            {
                Context = context;
            }

            public abstract string Write(char c);
        }

        class UppercaseState : TogleStateAbstract
        {
            public UppercaseState(TongleContext context) : base(context)
            {
            }

            public override string Write(char c)
            {
                if (char.IsLetter(c))
                {
                    Context.CurrentState = Context.LowercaseState;
                    return char.ToUpper(c, Context.CultureInfo).ToString();
                }

                return c.ToString();
            }
        }

        class LowercaseState : TogleStateAbstract
        {
            public LowercaseState(TongleContext context) : base(context)
            {
            }

            public override string Write(char c)
            {
                if (char.IsLetter(c))
                {
                    Context.CurrentState = Context.UppercaseState;
                    return char.ToLower(c, Context.CultureInfo).ToString();
                }

                return c.ToString();
            }
        }

        class TongleContext
        {
            public CultureInfo CultureInfo { get; }
            public  TogleStateAbstract CurrentState { get; set; }
            public TongleContext(CultureInfo cultureInfo)
            {
                CultureInfo = cultureInfo;
                LowercaseState = new LowercaseState(this);
                UppercaseState = new UppercaseState(this);
                CurrentState = LowercaseState;
            }
            
            public string Write(char c) => CurrentState.Write(c);

            public LowercaseState LowercaseState;
            public UppercaseState UppercaseState;
        }

        public CultureInfo Culture { get; }
        public string Name => "Tongle case";

        public string Execute(string input)
        {
#pragma warning disable 219
            var state = true;
#pragma warning restore 219
            var context = new TongleContext(Culture);
            return string.Join(string.Empty, input.Select(ch => context.Write(ch)));
            // return string.Join(string.Empty, input.Select(ch =>
            // {
            //     if (!char.IsLetter(ch))
            //     {
            //         return ch;
            //     }
            //     try
            //     {
            //         return state ? char.ToUpper(ch) : char.ToLower(ch);
            //     }
            //     finally
            //     {
            //         state = !state;
            //     }
            // }));
        }

        public override string ToString() => Name;
    }
}