using System.Collections.Generic;

namespace SaiyanTransformations
{
    /// <summary>One beat of a boss encounter, split in two voices:
    /// <list type="bullet">
    /// <item><description><see cref="Narration"/> - the external narrator, shown as a toast.
    /// Scene-setting and lore, never spoken by the boss or the player.</description></item>
    /// <item><description><see cref="Speech"/> - the boss's own words, shown in an NPC-style
    /// dialogue box with the boss's portrait.</description></item>
    /// </list>
    /// Either half may be null: wordless monsters get narration only.</summary>
    internal sealed class Beat
    {
        public readonly string Narration;
        public readonly string Speech;

        public Beat(string narration, string speech)
        {
            this.Narration = narration;
            this.Speech = speech;
        }
    }

    internal sealed class BossLines
    {
        public Beat Meet;         // encounter x1
        public Beat Rematch2;     // encounter x2
        public Beat Rematch3;     // encounter x3
        public Beat RematchLoop;  // encounter x4 and onward, repeatable
        public Beat Defeat;
    }

    /// <summary>The full script. The narrator carries the through-line: the wish scattered
    /// deep in the mine bleeds power up through the rock, a signal that crosses death and the
    /// seams between worlds, and it has drawn the fallen down to meet whoever is bending fate.</summary>
    internal static class BossDialogue
    {
        public static BossLines For(string id)
        {
            return id != null && Table.TryGetValue(id, out BossLines lines) ? lines : null;
        }

        // beat builders: N = narration only, S = speech only, B = both
        private static Beat N(string narration) => new Beat(narration, null);
        private static Beat S(string speech) => new Beat(null, speech);
        private static Beat B(string narration, string speech) => new Beat(narration, speech);

        private static readonly Dictionary<string, BossLines> Table =
            new Dictionary<string, BossLines>
            {
                // =========================================================== Saiyan saga
                ["Saibamen"] = new BossLines
                {
                    Meet = N("The soil splits. Green things claw up out of it - seeded here, long ago, to greet whatever came bending fate this deep."),
                    Rematch2 = N("The same crop, sprouting again. It remembers being cut."),
                    Rematch3 = N("The soil is barren now, yet still they come: fewer, meaner, grown wrong in the dark."),
                    RematchLoop = N("The dirt coughs up another handful of green spite. It will never stop sprouting for you."),
                    Defeat = N("The last Saibaman bursts like a struck gourd. Whatever seeded them now knows you are coming.")
                },
                ["Guldo"] = new BossLines
                {
                    Meet = B("A squat, four-eyed thing plants its feet - the Ginyu Force's smallest, and its first offering.",
                             "Guldo, of the Ginyu Force. I would stop time and end you politely, but the Captain insists we fight fair. Pity."),
                    Rematch2 = S("You beat ME? The others will never let me hear the end of it. Again!"),
                    Rematch3 = S("I have been practising holding my breath. Time will stop this time, digger."),
                    RematchLoop = S("The Force sends its smallest first. Do not mistake that for its weakest."),
                    Defeat = B("Guldo pops, out of time at last, and the trick of a thrown disc is left behind in your hands.",
                               "I needed... one more second...")
                },
                ["Nappa"] = new BossLines
                {
                    Meet = B("A mountain of a Saiyan rolls his neck, bored down to the bone.",
                             "Rotting down here, and finally someone worth standing up for. Don't die too fast - I've waited a long time."),
                    Rematch2 = S("You again. I've been training on the rocks. Let's see if it took."),
                    Rematch3 = S("Third round. I actually look forward to these. Don't tell anyone."),
                    RematchLoop = S("Same time as always, eh? Fine by me. Nappa doesn't get bored twice."),
                    Defeat = B("He topples, almost pleased to have lost to something real.",
                               "Tch... you're... actually strong-")
                },
                ["Jeice"] = new BossLines
                {
                    Meet = B("A red-skinned fighter flicks his white mane, half of a duo that is missing its other half.",
                             "The Red Magma, Jeice! Burter's around here somewhere. Let's see if you're worth the Crusher."),
                    Rematch2 = S("Back? Burter warned me you were stubborn. He's usually wrong. Not today."),
                    Rematch3 = S("Third go. I've stopped waiting for Burter and started fighting for real."),
                    RematchLoop = S("The Force never really disbands, mate. We just keep queueing up for you."),
                    Defeat = B("Jeice goes down alone, calling a name that does not answer.",
                               "Buuurter-! ...he's not coming, is he.")
                },
                ["EliteWarrior"] = new BossLines
                {
                    Meet = B("An elite Saiyan looks you over and refuses, pointedly, to raise his power.",
                             "A lowborn, glowing gold? You wear it like it's yours. I won't need to reach for mine, not for you."),
                    Rematch2 = S("I have replayed our fight a thousand times in this dark. This time I do not underestimate you."),
                    Rematch3 = S("You keep climbing down to me. Either you respect me, or you are a fool. Let's find out."),
                    RematchLoop = S("We are a ritual now, you and I. Draw your ki. I'll be waiting where I always am."),
                    Defeat = B("The arrogance goes out of him, and with it a way to cross distance in a blink - now yours.",
                               "Impossible... a third-class...")
                },
                ["Burter"] = new BossLines
                {
                    Meet = B("A tall blue fighter is suddenly, impossibly, already beside you.",
                             "Burter. Fastest in the universe. You won't see the hits - you'll just feel them, in order, very quickly."),
                    Rematch2 = S("Jeice fell to you. That makes this personal. And fast."),
                    Rematch3 = S("I've gotten faster. I am always getting faster. Try to keep up."),
                    RematchLoop = S("Blink and I've lapped you twice. Standard Force procedure."),
                    Defeat = B("Even Burter runs out of speed in the end.",
                               "...too... slow...")
                },
                ["Recoome"] = new BossLines
                {
                    Meet = B("A hulking orange brute strikes a pose, one leg raised, and holds it.",
                             "Naaame's Recoome, of the Ginyu Force! It rhymes with DOOM. You'll want to watch the whole routine."),
                    Rematch2 = S("You interrupted the routine last time. This time you watch it to the END."),
                    Rematch3 = S("I added new poses. TEN of them. You'll be here a while."),
                    RematchLoop = S("Recoome's Command Performance, encore number... I've lost count. Sit DOWN."),
                    Defeat = B("He goes down mid-flex, deeply, personally offended.",
                               "...my best... pose...")
                },
                ["CaptainGinyu"] = new BossLines
                {
                    Meet = B("He lands last of all, in perfect formation with no one, having watched you dismantle his Force one by one.",
                             "CAPTAIN GINYU! You've cut down my squad piece by piece. Now face the man who trained them - and mind you don't let me touch you."),
                    Rematch2 = S("You return, and my Force with grudges. Behold: the reunion special!"),
                    Rematch3 = S("Three times now. I could take your body and end this - but where is the showmanship in that?"),
                    RematchLoop = S("The Ginyu Force is eternal, digger. So is this routine."),
                    Defeat = B("The Captain topples, unable to swap bodies in time, and the wild long-maned fury he guarded pours into you.",
                               "A fine body, wasted on- urk.")
                },

                // =========================================================== Frieza saga
                ["FriezaFirst"] = new BossLines
                {
                    Meet = B("A small horned figure floats up, entirely unbothered by you.",
                             "My first form, and already past your reach. I have three more. You will not meet them all - but I may show you the light before you go."),
                    Rematch2 = S("Persistent vermin. I have not even changed shape for you. Yet."),
                    Rematch3 = S("Third time in my lowest form. You should feel insulted that it is enough."),
                    RematchLoop = S("I keep this shape for you especially. It amuses me to win small."),
                    Defeat = B("He recoils, astonished to be losing in this form, and the blinding flare he hoarded unfolds into your understanding.",
                               "Impossible... in this form...")
                },
                ["CoolerFirst"] = new BossLines
                {
                    Meet = B("Colder than the emperor, and far quieter, a fourth-form tyrant regards you with mild distaste.",
                             "Frieza announces himself. I simply arrive. You've been carving through my little brother's kind - now try his better half."),
                    Rematch2 = S("My brother's killer, back again. He would be jealous of the attention."),
                    Rematch3 = S("Third time. Frieza never learned a thing from losing to you. I am not Frieza."),
                    RematchLoop = S("The family business, it seems, is losing to you. I mean to break the tradition."),
                    Defeat = B("Cooler's certainty cracks, exactly as his brother's did, one floor above.",
                               "I am the superior brother. I am-")
                },
                ["FriezaFinal"] = new BossLines
                {
                    Meet = B("The armour is gone, the horns and bulk all shed - and in surpassing him you feel god ki answer, close and cold.",
                             "This is the shape that killed a planet. You climbed past my first form; you will not climb past this one."),
                    Rematch2 = S("You. The one who bloodied my first form and lived. This time I began at the end."),
                    Rematch3 = S("Third audience in my final form. Do you collect near-deaths?"),
                    RematchLoop = S("No more forms to hide behind, and still I am here. So are you. Curious."),
                    Defeat = B("He is wrong that there is nothing beyond - but that lesson waits on a deeper floor. Beating him, you take hold of god ki.",
                               "This is my FINAL form - there is nothing beyond-!")
                },
                ["CoolerFinal"] = new BossLines
                {
                    Meet = B("A form Frieza never reached uncoils itself, one step past the family's best.",
                             "My brother stopped at his final shape. I went further. Everything he was, and more."),
                    Rematch2 = S("You felled the form beyond my brother's best. I will simply have to be more."),
                    Rematch3 = S("Third meeting. I begin to think you are the family curse made flesh."),
                    RematchLoop = S("One form past Frieza, and still we share the same ending against you. Infuriating."),
                    Defeat = B("Final-form Cooler falls a single step past his brother, and no further.",
                               "The form beyond his... was not... enough...")
                },

                // =========================================================== Cell saga
                ["CellImperfect"] = new BossLines
                {
                    Meet = B("A hunched, insectoid thing crouches over the tunnel, still drinking the life out of the walls.",
                             "Not yet complete. But complete enough for you. Hold still - I only need a little more."),
                    Rematch2 = S("You interrupted my meal last time. I have found other sources. I am closer now."),
                    Rematch3 = S("Third time, and further along each time. Soon there will be no imperfect left to fight."),
                    RematchLoop = S("I am always becoming. You are always just in time to slow it. Barely."),
                    Defeat = N("The imperfect thing bursts before it can finish. Somewhere deeper, its perfected self stirs at the loss.")
                },
                ["CellSemiPerfect"] = new BossLines
                {
                    Meet = B("Taller now, almost handsome, badly balanced on a body one component short of whole.",
                             "One android from perfection, and you stand in the way of it. I have waited far too long to be complete."),
                    Rematch2 = S("Still one short, and still you come. I am so close I can taste the symmetry."),
                    Rematch3 = S("Third time in this half-finished shape. I begin to think you enjoy the incomplete."),
                    RematchLoop = S("Semi-perfect is still more than you will ever be. Remember that as you die."),
                    Defeat = N("The half-finished form ruptures. Perfection remains, for now, a floor deeper down.")
                },
                ["CellJuniors"] = new BossLines
                {
                    Meet = B("Small blue horrors spill out, all teeth and no mercy, spat from something that made them to play.",
                             "Father made us to practise on things that scream. You'll do."),
                    Rematch2 = N("They are bigger now. Practice does that."),
                    Rematch3 = N("Bigger still, and they have stopped screaming when they die. They learned that from you."),
                    RematchLoop = N("A fresh brood, each wearing a little of your own fighting style. Father sends his regards."),
                    Defeat = N("The last Junior pops. Somewhere, the thing that fathered them feels the loss, and is intrigued.")
                },
                ["CellPerfect"] = new BossLines
                {
                    Meet = B("It steps out flawless, symmetrical, smiling - the finished design, and it knows it.",
                             "Perfect. You met my lesser stages and lived; a mistake I have grown past. There is nothing incomplete about me anymore."),
                    Rematch2 = S("You unmade perfection once. It regenerated. It remembers. It improved."),
                    Rematch3 = S("Third bout with the complete article. Even perfect can be practised, it turns out."),
                    RematchLoop = S("I am the finished design, digger. You are a bug I keep having to close."),
                    Defeat = B("It regenerates once, twice, then not at all. Beating perfection teaches a stillness bluer than any rage.",
                               "I am PERFECT, I cannot-")
                },
                ["Bojack"] = new BossLines
                {
                    Meet = B("Broken chains hang from his wrists; the wish leaking up through the rock has loosened a very old seal.",
                             "Sealed away by cowards once. Your climbing has shaken the walls loose - and the first thing I spend my freedom on is you."),
                    Rematch2 = S("The seal is weaker every time you come. Soon I won't go back at all."),
                    Rematch3 = S("Third break-out. Keep coming, digger - you're loosening the chains for me."),
                    RematchLoop = S("The seal is a formality now. I stay dead only to enjoy escaping. For YOU."),
                    Defeat = B("His chains reform around a falling shadow, and the reckless red art of overload is left with you.",
                               "Back... in the dark...")
                },
                ["Broly"] = new BossLines
                {
                    Meet = B("A giant trembles at the tunnel's mouth, muttering one word over and over until he sees you and decides you will do.",
                             "The legend does not stop. The legend does not tire. The legend has found you."),
                    Rematch2 = N("The trembling is worse, the muttering louder. There is less of Broly left, and more of the legend."),
                    Rematch3 = N("He no longer says the name. He says nothing at all. There is nothing left in him to say it with."),
                    RematchLoop = N("The legend wears a shape that used to be a man. It knows only that you are here, and that is enough."),
                    Defeat = N("Broly's endless roar finally, briefly, ends.")
                },
                ["Dabura"] = new BossLines
                {
                    Meet = B("A demon king in a fine cloak considers you with contempt, called up from below by something hungrier than he is.",
                             "The Majin stirs deeper down and calls the worthy to die for it. I came gladly. Your soul will make a fine statue."),
                    Rematch2 = S("Death was only a door, digger. I walked back through it for you."),
                    Rematch3 = S("Hell is dull. You are not. I return, a third time, gladly."),
                    RematchLoop = S("The Demon Realm spits me back up whenever you dig this deep. We are cursed together, you and I."),
                    Defeat = B("Dabura crumbles to stone, cursing the master who spent him.",
                               "The Majin will... swallow you whole-")
                },

                // =========================================================== Buu saga
                ["BuuFat"] = new BossLines
                {
                    Meet = B("A round, pink, grinning thing bounces once. It does not understand what it is; that is the worst part.",
                             "Buu play now? Buu turn you into candy. Hee hee."),
                    Rematch2 = S("You hurt Buu. Buu remember hurt. Buu not so friendly this time."),
                    Rematch3 = S("Third time you make Buu angry. Angry Buu is a different Buu."),
                    RematchLoop = S("Buu always come back. Buu cannot be turned to candy. Only you can."),
                    Defeat = N("Fat Buu deflates with a childish wail - but the anger it swallowed does not die with it.")
                },
                ["SuperBuu"] = new BossLines
                {
                    Meet = B("A leaner, crueller pink shape tilts its head - the rage that split off from the fat one you met above.",
                             "You smell strong. When I eat you, I'll move like you, hit like you. Hold still - it only hurts until you're me."),
                    Rematch2 = S("I ate things stronger than last time. I am more now. Are you?"),
                    Rematch3 = S("Still won't hold still! Fine. I'll wear you down and wear you OUT."),
                    RematchLoop = S("You always come back tasty. One day I'll finish the meal. Today?"),
                    Defeat = N("Super Buu deflates with a long, disappointed sigh, having absorbed nothing at all.")
                },
                ["BuuSuperGohan"] = new BossLines
                {
                    Meet = B("Leaner still, and terribly quiet: this one ate a demigod, and it made him patient. In its calm you feel your own body begin to move without you.",
                             "The fat one raged, the hungry one copied. I ate better than either, and it made me still. I do not need to rage for you."),
                    Rematch2 = S("You. The others raged and lost. This one simply waits, and remembers you."),
                    Rematch3 = S("Third time against the still version of me. You have earned a calm I reserve for gods."),
                    RematchLoop = S("I have eaten better than you and grown quiet. Your return is the only thing that stirs me."),
                    Defeat = B("The absorbed calm shatters into a shriek. Surviving a thing this composed teaches the body to move on its own - instinct, ultra and clean.",
                               "This is not... how it ends for me-")
                },
                ["MetalCoolerLegion"] = new BossLines
                {
                    Meet = B("Cooler's face, over and over, in cold chrome, marching up the shaft in ranks.",
                             "You broke both his living forms above. The Big Gete Star kept the pattern regardless. Break one of me, and the next steps forward - forever."),
                    Rematch2 = S("The Star has fabricated improvements. You will not enjoy them."),
                    Rematch3 = S("Version three. We have studied every scratch you left on us. Efficiency improved."),
                    RematchLoop = S("The pattern is endless and the Star is patient. Break one Cooler, meet the next. Forever."),
                    Defeat = N("The last chrome Cooler seizes and dies. For now, the pattern has no more copies to spend.")
                },
                ["KidBuu"] = new BossLines
                {
                    Meet = B("Small, pink, grinning with nothing behind it - the original, before the fat, before the reason, before restraint.",
                             "..."),
                    Rematch2 = N("It reformed out of nothing, as it always does. It does not remember you; it never remembers anyone."),
                    Rematch3 = N("It has forgotten you again, utterly. To it, this is always the first time. That is the horror of it."),
                    RematchLoop = N("Pink, grinning, blank. It destroys because it destroys. Fight it a thousand times and be a stranger each one."),
                    Defeat = N("Kid Buu comes apart in a giggle and does not, this time, put itself back together.")
                },

                // =========================================================== the deep end
                ["FriezaGolden"] = new BossLines
                {
                    Meet = B("Gold light, and a familiar cruelty grown monstrous with training in the dark.",
                             "You met me weak, up in the shallows, and thought you understood me. Behold what an emperor becomes when he finally bothers to work for it."),
                    Rematch2 = S("You put down my golden form. Few can say that. None say it twice."),
                    Rematch3 = S("I have learned to hold the gold longer. Stamina was always my little flaw."),
                    RematchLoop = S("Down here, in gold, I have all the time in the world to keep meeting you."),
                    Defeat = B("Golden Frieza falls, and even falling he is certain there is a shade past gold still waiting below.",
                               "This is not the end of me. There is a colour past gold-")
                },
                ["FriezaBlack"] = new BossLines
                {
                    Meet = B("No aura at all. Just black, and quiet, and certain - the shape that surpassed everyone without a sound.",
                             "You have beaten every form I ever wore: first, final, gold. This is the last one. I did not train for gold. I trained for you."),
                    Rematch2 = S("You beat black. Then I was not yet black enough. Now I am."),
                    Rematch3 = S("Three times against the form that surpassed everyone. You are the exception I keep making."),
                    RematchLoop = S("There is nothing past this, digger. Only me, again, and again, and worse."),
                    Defeat = N("Black Frieza goes still without a sound. Far below, on a throne of fallen stone, a god takes notice.")
                },
                ["Destroyer"] = new BossLines
                {
                    Meet = B("A lean figure yawns on a throne of fallen stone at the bottom of the world. Every fallen thing above was only the road to him.",
                             "Mm. You woke me. Do you know how few things are permitted to do that? Let's see if you're a snack, or an insult."),
                    Rematch2 = S("Back? I did not destroy you last time out of curiosity. Do not expect it twice."),
                    Rematch3 = S("Three visits. I am beginning to think of you as a pet. Pets are permitted to live. Barely."),
                    RematchLoop = S("Ah, my recurring little insult. Sit. Fight. Amuse me. You have earned that much tedium."),
                    Defeat = B("For the first time in an age the god chooses to sit back down rather than end everything.",
                               "Interesting. Live, then. For now.")
                },

                // =========================================================== Dragon Ball guardians (wordless)
                ["BallGuardian1"] = new BossLines
                {
                    Meet = N("A hulking keeper coils around a single glowing sphere. It does not speak; it only tightens its grip."),
                    Rematch2 = N("The One-Star Ball has drifted back to the dark, and its keeper with it. It remembers your reach."),
                    Rematch3 = N("It has grown extra coils in your absence, all of them meant for you."),
                    RematchLoop = N("The guardian no longer guards the sphere so much as waits for you to come and take it. Again."),
                    Defeat = N("The guardian shatters and the One-Star Ball rolls free, warm in your hand.")
                },
                ["BallGuardian2"] = new BossLines
                {
                    Meet = N("A great serpent circles the Two-Star Ball, and an older thing rides its mind: a memory of light borrowed from every living being."),
                    Rematch2 = N("The sphere sits again in the coils. The guardian has not forgotten what you took from it."),
                    Rematch3 = N("The borrowed light it carries has learned to lash out first."),
                    RematchLoop = N("The serpent barely stirs. It simply waits, certain you will come to feed it your ki again."),
                    Defeat = N("The serpent uncoils in death, and the Two-Star Ball - and the memory of the spirit drawn from all life - are yours.")
                },
                ["BallGuardian3"] = new BossLines
                {
                    Meet = N("Two tireless keepers flank the Three-Star Ball, taking turns to watch. Neither has slept since it fell here."),
                    Rematch2 = N("The sphere returned; so did its watchers, angrier for the wait."),
                    Rematch3 = N("They no longer take turns. Both watch, always, for you."),
                    RematchLoop = N("Sleepless, endless, and yours to put down again. The Three-Star Ball is patient, and so are they."),
                    Defeat = N("Both keepers fall at last, and the Three-Star Ball is loosed from the dark.")
                },
                ["BallGuardian4"] = new BossLines
                {
                    Meet = N("Two serpents braid themselves around the Four-Star Ball - the sphere that once meant grandfather to someone, somewhere."),
                    Rematch2 = N("The braid has reformed around the sphere, tighter than before."),
                    Rematch3 = N("The serpents have learned your feints. They strike where you will be, not where you are."),
                    RematchLoop = N("The four-starred sphere returns to its coils each time. So do you. So do they."),
                    Defeat = N("The serpents fall away, and the Four-Star Ball, warm as a memory, is yours.")
                },
                ["BallGuardian5"] = new BossLines
                {
                    Meet = N("Three keepers stand shoulder to shoulder across the tunnel, the Five-Star Ball glinting behind them. No way around. Only through."),
                    Rematch2 = N("Three again, filling the tunnel, and this time they were expecting you."),
                    Rematch3 = N("They have packed the tunnel tighter. There is no seam to slip. Only through."),
                    RematchLoop = N("The wall of them reforms across the shaft each time you take the sphere. Break it. Again."),
                    Defeat = N("The wall of keepers comes down, and the Five-Star Ball rolls into the light.")
                },
                ["BallGuardian6"] = new BossLines
                {
                    Meet = N("Three mummified keepers circle the Six-Star Ball in a slow, endless procession, and the dust never settles."),
                    Rematch2 = N("The dance resumed the moment you left. It has been waiting to close around you again."),
                    Rematch3 = N("The procession has quickened. The dust no longer settles even between your visits."),
                    RematchLoop = N("Round and round, endless as your returns. Step in, take the sphere, leave. Repeat."),
                    Defeat = N("The procession halts. The Six-Star Ball lies still at its centre, waiting for your hand.")
                },
                ["BallGuardian7"] = new BossLines
                {
                    Meet = N("A mixed host guards the last sphere, as if the dark itself grew nervous about letting this one go. With it, the wish is whole."),
                    Rematch2 = N("The seventh is the hardest to keep and the hardest to take. Its guardians remember your face."),
                    Rematch3 = N("The host has grown. The dark does not want to lose this one twice."),
                    RematchLoop = N("The final sphere always returns to the deepest, angriest guard. Claim it again - and the wish with it."),
                    Defeat = N("The host falls in a tangle of shadow and scale. The Seven-Star Ball is yours - and with it, the wish.")
                }
            };
    }
}
