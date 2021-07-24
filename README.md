# CombatEffectsforCE

![Image](https://i.imgur.com/WAEzk68.png)

Update of CalaveraLocas mod
https://steamcommunity.com/sharedfiles/filedetails/?id=1756442393

- Added option to disable the extra blood-spatter

![Image](https://i.imgur.com/7Gzt3Rg.png)


[table]
	[tr]
		[td]https://invite.gg/Mlie]![Image](https://i.imgur.com/zdzzBrc.png)
[/td]
		[td]https://github.com/emipa606/CombatEffectsforCE]![Image](https://i.imgur.com/kTkpTOE.png)
[/td]
	[/tr]
[/table]
	
![Image](https://i.imgur.com/NOW7jU1.png)


This mod adds sparks to bullets impacting hard surfaces and blood effects for hitting pawns.
Now also allows bullets to penetrate materials.
Only works with Combat Extended.
IMPORTANT : Load it AFTER the Combat Extended mod.

**UPDATE** June 10 : 
- Bugfixes (Hanging projectiles, NaN projectile position, inconsistent projectile position for logic and graphics)

Plans: 
[strike]-Add a few more effects (like dust/smoke puffs to impacts) - DONE [/strike]
- If possible add bullet ricochet and material penetration. :
- - - Penetration initial implementation done. Details below.
- Vanilla version.
- Make options available to tone the effects up or down

**Penetration mechanics** :
Bullets have a chance to penetrate a build object based on caliber, ammo type and the material and condition of the object it hit.
Every projectile type has a base chance to penetrate a given material.

For example : a 9x19 FMJ has 10% chance to penetrate a wooden wall if that wall has 100% hitpoints.
It has 80% chance to penetrate that wall if it&apos;s below 50% hitpoints.
Meanwhile a 14.5x114 has 100% to penetrate that wall regardless it&apos;s hitpoints.

A steel wall is impenetrable by small arms when kept in good condition but the aforementioned 14.5 has 60% to penetrate it.

AP type bullet (Sabot, AP-I etc) has increased penetration stats.

Additionally, there is a slight randomness of these chances. So it&apos;s entirely possible for a .303 to penetrate a plasteel wall once in a lifetime.

However:
- HP bullets and energy projectiles never penetrate (for now).
- Arrows never penetrate (for now)

Bullets can penetrate pawns. It&apos;s a flat percentage. (Example 7.62x51 has 35% to penetrate the target).
So a singe .50BMG can ruin the day of several Alphabeavers in a bottleneck. But it can also ruin the day of allies behind those Alphabeavers.

Kinetic energy : WIP. Not enabled yet. But after each penetration or ricochet the bullet will loose some of it&apos;s energy so even if the projectile penetrates your walls it may do minimal damage.

Angle of Attack : WIP. Not enabled yet. Penetration chances as well as ricochet will additionally depend on the angle of incidence.

Important : I had to patch and decorate a lot of ammo defs for this. For now only ammo used by &apos;CE&apos; and &apos;CE Guns&apos; are patched. The others may be incompatible.




![Image](https://media.giphy.com/media/Tcgk85vK91UEzLoc1R/giphy.gif)



![Image](https://i.imgur.com/Rs6T6cr.png)



-  See if the the error persists if you just have this mod and its requirements active.
-  If not, try adding your other mods until it happens again.
-  Post your error-log using https://steamcommunity.com/workshop/filedetails/?id=818773962]HugsLib and command Ctrl+F12
-  For best support, please use the Discord-channel for error-reporting.
-  Do not report errors by making a discussion-thread, I get no notification of that.
-  If you have the solution for a problem, please post it to the GitHub repository.




