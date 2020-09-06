using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace ThePathBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class OwnerOrPermission : CheckBaseAttribute
    {
        public Permissions Permissions { get; private set; }

        public OwnerOrPermission(Permissions permissions)
        {
            this.Permissions = permissions;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var app = ctx.Client.CurrentApplication;
            var me = ctx.Client.CurrentUser;

            if (app != null && app.Owners.Contains(ctx.User))
                return Task.FromResult(true);

            if (ctx.User.Id == me.Id)
                return Task.FromResult(true);

            var usr = ctx.Member;
            if (usr == null)
                return Task.FromResult(false);
            var pusr = ctx.Channel.PermissionsFor(usr);

            return Task.FromResult((pusr & this.Permissions) == this.Permissions);
        }
    }
}
