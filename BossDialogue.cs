using System.Collections.Generic;

namespace SaiyanTransformations
{
    /// <summary>The lines a boss speaks (or the narration around it) on meeting, on defeat,
    /// and on each rematch. Kept as data, apart from the spawn logic, so the writing is easy
    /// to read and edit in one place.</summary>
    internal sealed class BossLines
    {
        public string[] Meet;         // encounter x1
        public string[] Rematch2;     // encounter x2
        public string[] Rematch3;     // encounter x3
        public string[] RematchLoop;  // encounter x4 and onward, repeatable
        public string[] Defeat;
    }

    internal static class BossDialogue
    {
        public static BossLines For(string id)
        {
            return id != null && Table.TryGetValue(id, out BossLines lines) ? lines : null;
        }

        private static string[] One(string s) => new[] { s };

        private static readonly Dictionary<string, BossLines> Table =
            new Dictionary<string, BossLines>
            {
                ["Saibamen"] = new BossLines
                {
                    Meet = One("The soil splits and green things claw up out of it, already reaching. Something deeper planted them here to greet the curious."),
                    Rematch2 = One("The ground churns again - the same crop, and it remembers being cut down."),
                    Rematch3 = One("The soil is barren now, yet still they come - fewer, meaner, grown wrong in the dark."),
                    RematchLoop = One("The dirt coughs up another handful of green spite. They will never stop sprouting for you."),
                    Defeat = One("The last Saibaman bursts like a struck gourd. Whatever seeded them now knows you are coming.")
                },
                ["CavernAmbush"] = new BossLines
                {
                    Meet = One("Pressed flat against the dark, waiting. Not for treasure - for a body warm enough to be worth the jump."),
                    Rematch2 = One("The dark bristles again. They have had time to learn where you last stood."),
                    Rematch3 = One("They no longer wait to be found. They come for the sound of your pick."),
                    RematchLoop = One("The shadows lunge the instant you arrive. This corner of the mine simply hates you now."),
                    Defeat = One("The ambush breaks. Whatever sent them ahead of itself learns you cannot be caught off guard.")
                },
                ["BladeAdepts"] = new BossLines
                {
                    Meet = One("Two figures rise, blades drawn. 'We were told a digger fights with light. Show us that trick - we collect such things.'"),
                    Rematch2 = One("'Back for another lesson? Good. We have sharpened since.'"),
                    Rematch3 = One("'Third time. We've stopped collecting your trick and started copying it.'"),
                    RematchLoop = One("'Edges out, digger. You know the drill by now - so do we.'"),
                    Defeat = One("'...The disc. Yes. That one.' They fall, and the knowing of it is left behind for you.")
                },
                ["Nappa"] = new BossLines
                {
                    Meet = One("A mountain of a Saiyan cracks his neck. 'Down here rotting, and finally someone worth standing up for. Don't die too fast - I've been bored a long time.'"),
                    Rematch2 = One("'You again. I've been training on the rocks. Let's see if it took.'"),
                    Rematch3 = One("'Third round. I actually look forward to these. Don't tell anyone.'"),
                    RematchLoop = One("'Same time as always, eh? Fine by me. Nappa doesn't get bored twice.'"),
                    Defeat = One("'Tch... you're... actually strong-' He topples, almost pleased.")
                },
                ["EliteWarrior"] = new BossLines
                {
                    Meet = One("'A lowborn, glowing gold? You wear it like it's yours.' He does not raise his power. 'I won't need to, for you.'"),
                    Rematch2 = One("'I have replayed our fight a thousand times. This time I do not underestimate you.'"),
                    Rematch3 = One("'You keep climbing down to me. Either you respect me, or you're a fool. Let's find out.'"),
                    RematchLoop = One("'We are a ritual now, you and I. Draw your ki. I'll be waiting where I always am.'"),
                    Defeat = One("'...Impossible. A third-class-' The arrogance goes out of him, and a way to cross distance in a blink goes into you.")
                },
                ["Recoome"] = new BossLines
                {
                    Meet = One("He strikes a pose, one leg up. 'Naaame's Recoome! Rhymes with DOOM. You'll want to watch the whole routine.'"),
                    Rematch2 = One("'You interrupted the routine last time. This time you watch it to the END.'"),
                    Rematch3 = One("'I added new poses. TEN of them. You'll be here a while.'"),
                    RematchLoop = One("'Recoome's Command Performance, encore number... I've lost count. Sit DOWN.'"),
                    Defeat = One("'...my best... pose...' He goes down mid-flex, deeply offended.")
                },
                ["BallGuardian1"] = new BossLines
                {
                    Meet = One("A hulking thing coils around a single glowing sphere. It does not speak - it only tightens its grip and fixes you with too many eyes."),
                    Rematch2 = One("The sphere returned to the dark, and so did its keeper. It remembers your reach."),
                    Rematch3 = One("It has grown extra coils in your absence, all of them for you."),
                    RematchLoop = One("The guardian no longer guards the sphere so much as waits for you to come take it. Again."),
                    Defeat = One("The guardian shatters and the One-Star Ball rolls free, warm in your hand.")
                },
                ["FriezaElites"] = new BossLines
                {
                    Meet = One("'The Emperor's vanguard, sent ahead to see what's been bending the readings this deep. You, obviously. He'll want to meet you personally - after we soften you up.'"),
                    Rematch2 = One("'He sent us again. He is nothing if not thorough.'"),
                    Rematch3 = One("'We asked for a transfer. He denied it. He says only you will do.'"),
                    RematchLoop = One("'Back on this cursed floor, dying for a man who won't come himself. Get it over with.'"),
                    Defeat = One("'Lord... Frieza... will hear of this-' The vanguard scatters like blown ash.")
                },
                ["GinyuSquad"] = new BossLines
                {
                    Meet = One("They land in formation, posing in sequence. 'GINYU FORCE! We heard a legend was digging around down here. We collect legends.'"),
                    Rematch2 = One("'New poses. New order. Let's hope not the same result for you.'"),
                    Rematch3 = One("'We've been practising the formation for nothing BUT you. Behold!'"),
                    RematchLoop = One("'GINYU FORCE, recurring engagement! You're practically an honorary member by now.'"),
                    Defeat = One("The formation collapses, poses and all. That long-maned fury you tapped to break them stays with you.")
                },
                ["Frieza"] = new BossLines
                {
                    Meet = One("A slim white figure hovers, smiling without warmth. 'So YOU are the disturbance in my kingdom of stone. I shall show you each of my forms - you won't survive to see the last.'"),
                    Rematch2 = One("'You. I have replayed your death in my mind. Let us make it real.'"),
                    Rematch3 = One("'A third audience? You are either very brave or you enjoy my company. I forgive neither.'"),
                    RematchLoop = One("'Ah. The disturbance returns, punctual as rot. I do so hate the punctual.'"),
                    Defeat = One("'This... this is MY power... how DARE-' His final form flickers and fails.")
                },
                ["KiAdepts"] = new BossLines
                {
                    Meet = One("Robed figures, light gathering unpleasantly in their hands. 'You carry techniques you have not earned. We'll take them back - starting with your sight.'"),
                    Rematch2 = One("'The light remembers you. It has been waiting to burn.'"),
                    Rematch3 = One("'We gather it faster now. Blink and you'll miss the last thing you ever see.'"),
                    RematchLoop = One("'Light gathers the moment you step in. You have taught it your shape.'"),
                    Defeat = One("As they fall, the blinding trick they hoarded unfolds into your understanding.")
                },
                ["Cooler"] = new BossLines
                {
                    Meet = One("Colder than his brother, and worse. 'Frieza always did announce himself. I prefer to simply arrive. You've been troubling my family's dominion, little digger.'"),
                    Rematch2 = One("'My brother fell to you and learned nothing. I am not my brother.'"),
                    Rematch3 = One("'You are becoming a habit. I do not have habits. I will END this one.'"),
                    RematchLoop = One("'Punctual again. Cold comfort - but then, I am nothing but cold.'"),
                    Defeat = One("'...I am the superior form. I am-' Cooler's certainty cracks and falls.")
                },
                ["BallGuardian2"] = new BossLines
                {
                    Meet = One("A great serpent coils around the sphere, and something older rides its mind - a memory of borrowed light, of power gathered from every living thing."),
                    Rematch2 = One("The sphere sits again in the coils. The guardian has not forgotten what you took."),
                    Rematch3 = One("The borrowed light it carries has learned to lash out first now."),
                    RematchLoop = One("The serpent barely stirs - it simply waits, certain you'll come to feed it your ki again."),
                    Defeat = One("The serpent uncoils in death, and the ball - and the memory of the spirit drawn from all life - are yours.")
                },
                ["Emperor"] = new BossLines
                {
                    Meet = One("Gold light fills the shaft. 'You've climbed past my soldiers and my brother, and still you come. This is not even my final form. Kneel, and I may make your end quick.'"),
                    Rematch2 = One("'I have polished this form to a mirror shine. Look into it. See your end.'"),
                    Rematch3 = One("'You keep returning to a god. Perhaps you wish to be one. You will die a mortal instead.'"),
                    RematchLoop = One("'Kneel, rise, kneel again - this is the shape of our forever. I never tire of it.'"),
                    Defeat = One("'The universe... was MINE...' The golden emperor gutters out. In beating a god, you learn to stand like one.")
                },
                ["CellJuniors"] = new BossLines
                {
                    Meet = One("Small blue horrors, all teeth and no mercy. 'Father made us to practice on things that scream. You'll do.'"),
                    Rematch2 = One("They are bigger now. Practice does that."),
                    Rematch3 = One("Bigger still, and they've stopped screaming when they die. They've learned from you."),
                    RematchLoop = One("A fresh brood, each wearing a little of your fighting style. Father sends his regards."),
                    Defeat = One("The last Junior pops. Somewhere, whatever fathered them feels the loss and is intrigued.")
                },
                ["BallGuardian3"] = new BossLines
                {
                    Meet = One("Two shadow-brutes flank the sphere, tireless, taking turns to watch. Neither has slept since it fell here."),
                    Rematch2 = One("The sphere returned; so did its watchers. They are angrier for the wait."),
                    Rematch3 = One("They no longer take turns. Both watch, always, for you."),
                    RematchLoop = One("Sleepless, endless, and yours to put down again. The Three-Star Ball is patient, and so are they."),
                    Defeat = One("Both keepers fall at last, and the Three-Star Ball is loosed from the dark.")
                },
                ["Dabura"] = new BossLines
                {
                    Meet = One("A demon king in a fine cape regards you with contempt. 'The Majin stirs below and calls the worthy down to die for it. Your soul will make fine stone.'"),
                    Rematch2 = One("'Death was a door, digger. I walked back through it for you.'"),
                    Rematch3 = One("'Hell is dull. You are not. I return gladly, a third time.'"),
                    RematchLoop = One("'The Demon Realm spits me back whenever you dig this deep. We are cursed together.'"),
                    Defeat = One("'The Majin will... swallow you whole-' Dabura crumbles, spitting curses.")
                },
                ["Ascetics"] = new BossLines
                {
                    Meet = One("Silent monks around a cache they'll never open. They rise as one. 'We guard, we do not want. You want. That is the difference we will settle.'"),
                    Rematch2 = One("They have sat here deepening their calm, sharpening it into a weapon aimed at you."),
                    Rematch3 = One("Their stillness has become a kind of violence. They rise before you finish arriving."),
                    RematchLoop = One("They have made a meditation of your visits. You are their koan, and they will solve you."),
                    Defeat = One("The last Ascetic bows its head and falls, and the cache is wordlessly yours.")
                },
                ["PerfectAndroid"] = new BossLines
                {
                    Meet = One("A green-plated figure smiles with too many teeth. 'Perfect. That is the word for me. I regenerate, I adapt, I do not tire. You are, at best, practice.'"),
                    Rematch2 = One("'I have perfected myself further. You have merely aged.'"),
                    Rematch3 = One("'Every defeat is data. I have a great deal of data on you now.'"),
                    RematchLoop = One("'Perfection is iterative, it seems. Thank you for the test cycles. Again.'"),
                    Defeat = One("'I am PERFECT, I cannot-' It regenerates once, twice, then not at all. Beating perfection teaches you a stillness bluer than rage.")
                },
                ["Bojack"] = new BossLines
                {
                    Meet = One("Chains hang broken from his wrists. 'Sealed away by cowards once. The wish leaking up through this rock loosened the seal - and the first thing I'll spend it on is you.'"),
                    Rematch2 = One("'The seal is weaker every time. Soon I won't go back at all.'"),
                    Rematch3 = One("'Third break-out. The chains barely hold. Keep coming - you're loosening them for me.'"),
                    RematchLoop = One("'The seal is a formality now. I stay dead only to enjoy escaping. For YOU.'"),
                    Defeat = One("'Back... in the dark...' His chains reform around a falling shadow.")
                },
                ["CrimsonMaster"] = new BossLines
                {
                    Meet = One("Wreathed in red flame, burning himself to burn brighter. 'Multiply your power at the cost of your body. Beat me, and the technique is yours to ruin yourself with.'"),
                    Rematch2 = One("'Still standing, this side of the fire. Let us see who burns brightest now.'"),
                    Rematch3 = One("'I should be ash by now. Spite keeps me lit. Spite, and the chance to fight you again.'"),
                    RematchLoop = One("'Every time I burn to nothing, every time I'm back. The fire likes you. So do I.'"),
                    Defeat = One("'Worth it... to pass it on...' He burns to nothing, grinning, and the crimson art is yours.")
                },
                ["BallGuardian4"] = new BossLines
                {
                    Meet = One("Two serpents braid around the sphere - the one that once meant grandfather to someone, somewhere. They will not part with it easily."),
                    Rematch2 = One("The braid has reformed around the sphere, tighter than before."),
                    Rematch3 = One("The serpents have learned your feints. They strike where you'll be, not where you are."),
                    RematchLoop = One("The four-starred sphere returns to its coils each time. So do you. So do they."),
                    Defeat = One("The serpents fall away and the Four-Star Ball, warm as a memory, is yours.")
                },
                ["SuperBuu"] = new BossLines
                {
                    Meet = One("A tall pink thing tilts its head. 'You smell strong. When I eat you, I'll move like you, hit like you. Hold still - it only hurts until you're me.'"),
                    Rematch2 = One("'I ate things stronger than last time. I am more now. Are you?'"),
                    Rematch3 = One("'Still won't hold still! Fine. I'll wear you down and wear you OUT.'"),
                    RematchLoop = One("'You always come back tasty. One day I'll finish the meal. Today?'"),
                    Defeat = One("Super Buu deflates with a long, disappointed sigh, unabsorbed.")
                },
                ["Majin"] = new BossLines
                {
                    Meet = One("Something that should not be. It giggles, childlike, and the shaft goes cold. 'Everyone came down to fight it. It ate them. Now it will play with you.'"),
                    Rematch2 = One("It remembers you. It did not think anything could be remembered. That has made it angry."),
                    Rematch3 = One("It has stopped giggling. In all its formless years, only you have earned its full attention."),
                    RematchLoop = One("The shaft goes cold the instant you enter. It has been waiting. It is always waiting. For you."),
                    Defeat = One("The Majin unmakes itself with a shriek. Surviving a thing beyond reason teaches your body to move without you - instinct, ultra and clean.")
                },
                ["BallGuardian5"] = new BossLines
                {
                    Meet = One("Three brutes across the tunnel, the sphere glinting behind them. No way around. Only through."),
                    Rematch2 = One("Three again, filling the tunnel, and this time they were expecting you."),
                    Rematch3 = One("They've packed the tunnel tighter. There is no seam to slip. Only through."),
                    RematchLoop = One("The wall of them reforms across the shaft each time you take the sphere. Break it. Again."),
                    Defeat = One("The wall of keepers comes down, and the Five-Star Ball rolls into the light.")
                },
                ["MetalCoolerLegion"] = new BossLines
                {
                    Meet = One("Cooler's face, over and over, in cold chrome. 'You destroyed the flesh. The Big Gete Star kept the pattern. Break one of me and another steps forward - forever, digger.'"),
                    Rematch2 = One("'The Star has fabricated improvements. You will not enjoy them.'"),
                    Rematch3 = One("'Version three. We have studied every scratch you've left on us. Efficiency improved.'"),
                    RematchLoop = One("'The pattern is endless and the Star is patient. Break one Cooler, meet the next. Forever.'"),
                    Defeat = One("The last chrome Cooler seizes and dies. For now, the pattern has no more copies to spend.")
                },
                ["Vanishers"] = new BossLines
                {
                    Meet = One("You never quite see them arrive. 'We are not here to guard, or to rule,' a voice says from three places at once. 'We are here because you are the only thing down here worth killing.'"),
                    Rematch2 = One("Quieter now. You'll feel them before you see them, if you're lucky."),
                    Rematch3 = One("They've stopped speaking entirely. There is only the cold spot at your back."),
                    RematchLoop = One("You never see them arrive and you never will. They only ever leave - one body short."),
                    Defeat = One("The Vanishers flicker out one last time and do not flicker back.")
                },
                ["BallGuardian6"] = new BossLines
                {
                    Meet = One("Three mummified keepers circle the sphere in a slow, endless procession, dust never settling. They simply add you to the dance."),
                    Rematch2 = One("The dance resumed the moment you left. It has been waiting to close around you again."),
                    Rematch3 = One("The procession has quickened. The dust no longer settles even between your visits."),
                    RematchLoop = One("Round and round, endless as your returns. Step in. Take the sphere. Leave. Repeat."),
                    Defeat = One("The procession halts. The Six-Star Ball lies still at its centre, waiting for your hand.")
                },
                ["Broly"] = new BossLines
                {
                    Meet = One("A giant, trembling, muttering one word - 'Kakarot' - until he sees you and decides you'll do. 'The legend does not stop. The legend does not tire. The legend has found you.'"),
                    Rematch2 = One("The trembling is worse, the word louder. There is less of Broly left, and more of the legend."),
                    Rematch3 = One("He no longer says 'Kakarot.' He says nothing. There is nothing left in him to say it."),
                    RematchLoop = One("The legend wears a shape that used to be a man. It knows only that you are here - and that is enough."),
                    Defeat = One("Broly's endless roar finally, briefly, ends.")
                },
                ["BallGuardian7"] = new BossLines
                {
                    Meet = One("A mixed host guards the last sphere, as if the dark itself grew nervous about letting it go. 'The seventh. With it, the wish is whole. You will not carry it up.'"),
                    Rematch2 = One("The seventh is the hardest to keep and the hardest to take. Its guardians remember your face."),
                    Rematch3 = One("The host has grown. The dark does not want to lose this one twice."),
                    RematchLoop = One("The final sphere always returns to the deepest, angriest guard. Claim it again - and the wish with it."),
                    Defeat = One("The host falls in a tangle of shadow and scale. The Seven-Star Ball is yours - and with it, the wish.")
                },
                ["KidBuu"] = new BossLines
                {
                    Meet = One("Small, pink, grinning with no thought behind it at all. The original - before the fat, before the reason. It destroys because destroying is all it has ever known."),
                    Rematch2 = One("It reformed out of nothing, as always. It doesn't remember you - it never remembers anyone."),
                    Rematch3 = One("It has forgotten you again, utterly. To it, this is always the first time. That is the horror."),
                    RematchLoop = One("Pink, grinning, blank. It kills because it kills. Fight it a thousand times and be a stranger each one."),
                    Defeat = One("Kid Buu comes apart in a giggle and does not, this time, put itself back together.")
                },
                ["Destroyer"] = new BossLines
                {
                    Meet = One("A lean figure yawns on a throne of fallen stone. 'Mm. You woke me. Do you know how few things are permitted to do that? You should not have come this deep. Let's see if you're a snack or an insult.'"),
                    Rematch2 = One("'Back? I did not destroy you last time out of curiosity. Do not expect it twice.'"),
                    Rematch3 = One("'Three visits. I am beginning to think of you as a pet. Pets are permitted to live. Barely.'"),
                    RematchLoop = One("'Ah, my recurring little insult. Sit. Fight. Amuse me. You've earned that much tedium.'"),
                    Defeat = One("The god blinks, genuinely surprised, and for the first time in an age chooses to sit back down rather than end everything. 'Interesting. Live, then. For now.'")
                }
            };
    }
}
