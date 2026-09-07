using System.Collections.Generic;
using StardewValley;

namespace SaiyanTransformations
{
    /// <summary>One beat of a boss encounter, in up to three voices, shown in this order:
    /// <list type="bullet">
    /// <item><description><see cref="Narration"/> - the narrator, a plain dialogue box.
    /// Scene-setting only; never spoken by anyone.</description></item>
    /// <item><description><see cref="Speech"/> - the boss, with its portrait.</description></item>
    /// <item><description><see cref="Player"/> - the farmer answering back, with their own
    /// portrait, so an encounter reads as a conversation instead of a monologue.</description></item>
    /// </list>
    /// Any of the three may be null.</summary>
    internal sealed class Beat
    {
        public readonly string Narration;
        public readonly string Speech;
        public readonly string Player;

        public Beat(string narration, string speech, string player = null)
        {
            this.Narration = narration;
            this.Speech = speech;
            this.Player = player;
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

    /// <summary>The script.
    ///
    /// THE STORY, plainly:
    ///
    /// Something at the bottom of the mine is torn. The wall between the living world and
    /// wherever the dead go has worn through down there, and seven wish-granting spheres fell
    /// through it long ago and lodged at intervals up the shaft. Their power is what holds the
    /// tear open.
    ///
    /// Three things follow from that, and every boss states one of them out loud:
    ///  1. The dead leak back in. Fighters who died elsewhere reform inside the mine, and the
    ///     tear only holds them as far as their own floor - none of them can climb, none can
    ///     leave. Kill one and it reforms in about forty days, angrier and stronger.
    ///  2. The tear leaks power upward. That is what is changing the player: the ki, the forms.
    ///     It is on loan from the hole in the world.
    ///  3. All seven spheres together grant a wish, and a wish would put any one of them back
    ///     in the living world properly. None of them can carry the spheres past their own
    ///     floor. The player can. That is the only reason they are all so interested.
    ///
    /// The cost: every wish widens the tear. Bojack says so out loud at floor 200, and that is
    /// the turn of the story - after it, the player knows their wishes are the thing letting
    /// worse things through, and keeps going anyway.
    ///
    /// The God of Destruction at the bottom was supposed to erase this wound before it started
    /// letting the dead back in. He decided watching was more interesting. He is the reason any
    /// of it happened, and beating him does not make him fix it.
    ///
    /// The Multiversal Invader (see Invader.cs) is what the widened tear finally lets through:
    /// something alive, from a universe that already ended.
    ///
    /// The player is a farmer. They answer everything plainly, get steadily more tired and more
    /// certain, and never once talk like a hero.</summary>
    internal static class BossDialogue
    {
        public static BossLines For(string id)
        {
            return id != null && Table.TryGetValue(id, out BossLines lines) ? lines : null;
        }

        /// <summary>A short narrator line for the moment a Mummy-type boss reforms after being
        /// knocked down. Each such boss has its own; anything without falls back to a generic.</summary>
        public static string ReviveLine(string id)
        {
            if (id != null && Revives.TryGetValue(id, out string[] lines) && lines.Length > 0)
                return lines[Game1.random.Next(lines.Length)];
            return "It drags itself back together and rises.";
        }

        private static readonly Dictionary<string, string[]> Revives =
            new Dictionary<string, string[]>
            {
                ["CellImperfect"] = new[]
                {
                    "Imperfect Cell oozes back into shape. It is still hungry.",
                    "You cannot kill what has not finished becoming.",
                },
                ["CellSemiPerfect"] = new[]
                {
                    "Semi-Perfect Cell reknits itself, furious at the interruption.",
                    "So close to whole - it will not stop one component short.",
                },
                ["CellPerfect"] = new[]
                {
                    "Perfect Cell reassembles without a mark. Of course it does.",
                    "Every cell remembers its shape. It simply puts itself back.",
                },
                ["BuuFat"] = new[]
                {
                    "The pink scatter pulls itself together. Fat Buu giggles.",
                    "Buu is not finished playing.",
                },
                ["SuperBuu"] = new[]
                {
                    "Super Buu flows back into one piece, smiling wider.",
                    "The steam gathers, hardens, and Super Buu stands up again.",
                },
                ["BuuSuperGohan"] = new[]
                {
                    "Super Buu reforms, calm and unhurried. It has all the time there is.",
                    "The pieces knit themselves back without any effort at all.",
                },
                ["MetalCoolerLegion"] = new[]
                {
                    "The machine below feeds the broken pattern back. Another Cooler stands up.",
                    "The Big Gete Star does not run out of copies.",
                },
                ["KidBuu"] = new[]
                {
                    "Kid Buu shrieks with laughter and pops back into shape.",
                    "There is no killing the original. It reforms, cackling.",
                },
            };

        // beat builders. N = narrator, S = boss, P = player.
        private static Beat N(string narration) => new Beat(narration, null, null);
        private static Beat NP(string narration, string player) => new Beat(narration, null, player);
        private static Beat SP(string speech, string player) => new Beat(null, speech, player);
        private static Beat NSP(string narration, string speech, string player)
            => new Beat(narration, speech, player);
        private static Beat NS(string narration, string speech) => new Beat(narration, speech, null);

        private static readonly Dictionary<string, BossLines> Table =
            new Dictionary<string, BossLines>
            {
                // ====================================================== floors 10-90: the Force
                ["Saibamen"] = new BossLines
                {
                    Meet = NP("The tunnel floor splits and green things haul themselves out of it, screeching. Six of them, in a row, all facing the way down.",
                              "These were planted. Facing up the shaft, like a fence. Someone down there wanted a warning line."),
                    Rematch2 = NP("The same green crop, pushing up through the same holes.",
                                  "Forty days and they are back. Nothing down here stays dead. I should have expected that by now."),
                    Rematch3 = NP("Fewer this time, and bigger. Whatever is in the soil is feeding them.",
                                  "They are growing on whatever leaks up from below. Same as everything else down here."),
                    RematchLoop = NP("The dirt coughs up another handful of green spite.",
                                     "Every season, same fence. Fine. I know the way through it."),
                    Defeat = NP("The last one bursts. Below the broken soil the shaft keeps going down.",
                                "That was not a guard. That was a fence, and I have just walked through it.")
                },
                ["Guldo"] = new BossLines
                {
                    Meet = NSP("A short, four-eyed soldier is standing at the bottom of the ladder with his arms folded, as if he has been there for years.",
                               "Guldo. Ginyu Force. Spare me the confusion - I died a very long way from this rock. The hole at the bottom of your mine spat me back out, and it will not let me climb one floor higher than this one.",
                               "So you are stuck. On floor twenty. That is why you are still here."),
                    Rematch2 = SP("You again. Do you have any idea how long forty days is when you cannot leave one room?",
                                  "About a growing season. You get used to those."),
                    Rematch3 = SP("I have had nothing to do but practise holding my breath. Today time stops.",
                                  "It did not stop the last two times either."),
                    RematchLoop = SP("The Force sends its smallest up first. That is not an insult, it is a rota.",
                                     "Same floor, same fight. Let us get on with it."),
                    Defeat = NSP("Guldo comes apart, and the trick of a thrown disc is left behind in your hands.",
                                 "One... more... second...",
                                 "You had forty days. Spend the next lot better.")
                },
                ["Nappa"] = new BossLines
                {
                    Meet = NSP("An enormous Saiyan is sitting against the wall with his eyes shut. He does not get up until he is certain you are real.",
                               "Name is Nappa. Do you know what it is like being dead in a hole with nothing worth hitting? I have been counting rocks. Do not die quickly - I have waited a long time for this.",
                               "I came down here for copper."),
                    Rematch2 = SP("Good. You are back. I have been punching the wall for practice and the wall is getting boring.",
                                  "You hit harder than last time. The wall must be helping."),
                    Rematch3 = SP("Third time. Honestly? This is the best part of being dead.",
                                  "That is a bleak thing to say, Nappa."),
                    RematchLoop = SP("Same floor, same season. I do not get bored twice.",
                                     "Neither do I, apparently."),
                    Defeat = NSP("He topples over, almost pleased about it.",
                                 "Tch. You are... actually strong.",
                                 "Get some rest. You will be back in a month.")
                },
                ["Jeice"] = new BossLines
                {
                    Meet = NSP("A red-skinned fighter is pacing the same twenty feet of tunnel, glancing over his shoulder for someone who never arrives.",
                               "Jeice. The Red Magma. My partner is two floors down and I cannot reach him - none of us can move off our own level. I have been shouting his name for a year. You will have to do instead.",
                               "You are all penned in separately. That is deliberate."),
                    Rematch2 = SP("Still cannot get to him. Still can get to you.",
                                  "If I reach him, I will tell him you are shouting."),
                    Rematch3 = SP("Did you see him? Is he still fast?",
                                  "He is still fast. He said he can hear you some nights."),
                    RematchLoop = SP("Tell him I am holding my floor. Tell him I said it properly.",
                                     "I will tell him. I always do."),
                    Defeat = NSP("Jeice goes down alone, calling a name that does not answer.",
                                 "Buuurter - ... he cannot hear me, can he.",
                                 "No. But he is still down there, and he is still holding his.")
                },
                ["EliteWarrior"] = new BossLines
                {
                    Meet = NSP("A Saiyan in cracked armour watches you come down the ladder, and very deliberately does not raise his power.",
                               "A farmhand wearing gold. Do you even know what that is? It is the shaft doing it to you. The deeper you go the more the hole down there pushes into you. Mine I earned. I will not need to reach for it.",
                               "So it is the mine. That is what has been happening to me."),
                    Rematch2 = SP("I have replayed our fight a thousand times down here. There is nothing else to do. This time I do not underestimate you.",
                                  "Then we are even. I have been training too."),
                    Rematch3 = SP("You keep climbing down to me. That is either respect or stupidity.",
                                  "It is habit. Most farming is."),
                    RematchLoop = SP("We are a ritual now, you and I. Draw your ki.",
                                     "Same place, same time. Go on then."),
                    Defeat = NSP("The arrogance goes out of him, and a way of crossing distance in a blink goes out with it, into you.",
                                 "Impossible. A third-class-",
                                 "I am a farmer. I am not even that.")
                },
                ["Burter"] = new BossLines
                {
                    Meet = NSP("The tunnel is empty. Then it is not: a tall blue fighter is standing beside you, and was not, a moment ago.",
                               "Burter. Fastest in the universe, and it does me no good whatsoever. I can cross this floor a thousand times a second and I still cannot reach the next one. Jeice is up there. I hear him some nights.",
                               "He is still calling for you. I told him you were down here."),
                    Rematch2 = SP("You told him. He stopped shouting for a week. Then he started again.",
                                  "He is stubborn. You would like that about him if you could hear it."),
                    Rematch3 = SP("Faster now. I am always getting faster. It never gets me anywhere.",
                                  "I know that feeling better than I would like to."),
                    RematchLoop = SP("Blink and I have lapped you twice. It is the only thing I have got left.",
                                     "It is a good thing to have. Come on."),
                    Defeat = NSP("Even Burter runs out of speed eventually.",
                                 "...too... slow...",
                                 "I will tell him you held your floor. He will want to know.")
                },
                ["Recoome"] = new BossLines
                {
                    Meet = NSP("An enormous orange fighter is mid-pose when you arrive, and holds it, clearly waiting for you to appreciate it.",
                               "Naaame is RECOOME! Rhymes with DOOM! Nobody has watched the routine in forty years down here, so you are going to watch ALL of it.",
                               "...Do I have to watch all of it?"),
                    Rematch2 = SP("You interrupted last time. This time you watch it to the END.",
                                  "Fine. Show me the routine."),
                    Rematch3 = SP("I have added ten new poses. TEN.",
                                  "You have had a great deal of time down here, haven't you."),
                    RematchLoop = SP("Command Performance, encore number - I have lost count. SIT DOWN.",
                                     "I am sitting. Go on then."),
                    Defeat = NSP("He goes down mid-flex, personally offended.",
                                 "...my best... pose...",
                                 "It was a good pose. I mean that.")
                },
                ["CaptainGinyu"] = new BossLines
                {
                    Meet = NSP("He lands in perfect formation with absolutely no one, having felt every one of his squad go down on the floors above him.",
                               "CAPTAIN GINYU. I felt them fall. Guldo, Jeice, Burter, Recoome, one floor at a time, and I could not climb a single step to help any of them. You did that. So you will understand my enthusiasm.",
                               "They all told me the same thing. That they could not reach each other."),
                    Rematch2 = SP("My Force reforms with me, floor by floor, each of them with a grudge. Behold: the reunion special!",
                                  "They are not up there with you, Captain. They are alone, same as you."),
                    Rematch3 = SP("Third time. I could take your body and walk out of this mine wearing it.",
                                  "Then why haven't you?"),
                    RematchLoop = SP("Because the hole will not let me leave in any body but my own. Believe me, I have tried it.",
                                     "So we are both stuck. Just differently."),
                    Defeat = NSP("The Captain topples, unable to swap out in time, and a wild long-maned fury pours into you.",
                                 "A fine body - wasted on-",
                                 "It was never the body you wanted. It was the way out.")
                },

                // ====================================================== floors 100-150: the family
                ["FriezaFirst"] = new BossLines
                {
                    Meet = NSP("A small horned figure floats a foot off the tunnel floor, entirely unbothered by you.",
                               "You have been climbing down murdering my employees. How industrious. I am Frieza. This is the smallest shape I own, and down here it is still more than enough for a farmhand.",
                               "You are pinned to this floor like the rest of them. That is why you are only this."),
                    Rematch2 = SP("Persistent vermin. I have not even changed shape for you.",
                                  "You cannot. Not this high up. The strong ones are further down - I have been counting."),
                    Rematch3 = SP("A third time in my lowest form. You should be insulted that it suffices.",
                                  "I am not insulted. I am taking notes."),
                    RematchLoop = SP("I keep this shape for you especially. It amuses me to win small.",
                                     "You have not won one yet."),
                    Defeat = NSP("He recoils, astonished, and a blinding flare unfolds into your understanding.",
                                 "Impossible - in this form-",
                                 "There are more of you further down, aren't there.")
                },
                ["CoolerFirst"] = new BossLines
                {
                    Meet = NSP("Colder than his brother and much quieter. He does not bother announcing himself.",
                               "Frieza announces. I arrive. You have been carving through my brother's leavings, and I am the better line of the family.",
                               "Brothers. Two floors apart. And neither of you can climb to the other."),
                    Rematch2 = SP("My brother's killer, back again. He would be jealous of the attention.",
                                  "He is two floors up. You could tell him yourself, if you could walk it."),
                    Rematch3 = SP("Frieza learned nothing from losing to you. I am not Frieza.",
                                  "You keep saying that. He keeps saying it too."),
                    RematchLoop = SP("The family business appears to be losing to you. I intend to break the tradition.",
                                     "Same time next season, then."),
                    Defeat = NSP("Cooler's certainty cracks, exactly the way his brother's did, two floors up.",
                                 "I am the superior brother. I am-",
                                 "You are both in the same hole. That is the whole family resemblance.")
                },
                ["FriezaFinal"] = new BossLines
                {
                    Meet = NSP("The armour is gone, and the horns, and the bulk. What is left is small, white and completely still - and standing near it you feel something old answer in your chest.",
                               "You have spheres, farmer. Seven of them makes a wish, and a wish would put any one of us back in the living world properly. None of us can carry them past our own floor. You can. That is the only reason you are still breathing this deep.",
                               "So none of you actually want me dead. You want me finished."),
                    Rematch2 = SP("The one who bloodied my first form and lived. This time I began at the end.",
                                  "You are stronger this far down. All of you are. It is the hole, not you."),
                    Rematch3 = SP("Do you collect near-deaths, farmer?",
                                  "I collect the spheres. You are standing between me and one."),
                    RematchLoop = SP("No forms left to hide behind, and still I am here. So are you.",
                                     "Neither of us gets to stop. That is the arrangement."),
                    Defeat = NSP("He is wrong that there is nothing beyond - that lesson is waiting further down. Beating him, you take hold of god ki.",
                                 "This is my FINAL form - there is nothing beyond-!",
                                 "There is a gold one. And a black one. Your employees talk.")
                },
                ["CoolerFinal"] = new BossLines
                {
                    Meet = NSP("A shape his brother never managed uncoils in the dark.",
                               "Frieza stopped at his final form. I did not. Everything he was, and one step further.",
                               "And still one floor, same as him. One step further into the same hole."),
                    Rematch2 = SP("You felled the form past my brother's best. I shall simply have to be more.",
                                  "There is always more down here. That is the problem with this place."),
                    Rematch3 = SP("I begin to think you are the family curse made flesh.",
                                  "I am a farmer with a pickaxe and a schedule."),
                    RematchLoop = SP("One form past Frieza, and the same ending against you every time. Infuriating.",
                                     "Tell him about it. Oh - you cannot."),
                    Defeat = NSP("Final-form Cooler falls one step past his brother, and no further.",
                                 "The form beyond his... was not... enough...",
                                 "It never is. Not down here.")
                },

                // ====================================================== floors 170-190: the design
                ["CellImperfect"] = new BossLines
                {
                    Meet = NSP("Something hunched and insectoid is crouched against the wall with its tail buried in the rock, drinking.",
                               "Do not stop me. I am drinking what leaks up this shaft and I am nearly whole. Everything down here feeds on it. You are simply the first thing that has fed on it and kept walking.",
                               "That is what has been changing me. It is changing you too. We are drinking from the same wound."),
                    Rematch2 = SP("You interrupted my meal. I found other seams. I am closer now.",
                                  "You are all feeding on the same hole. None of you seem to mind sharing."),
                    Rematch3 = SP("Further along each time. Soon there is no imperfect left to fight.",
                                  "Then I will keep coming back before then."),
                    RematchLoop = SP("I am always becoming. You are always just in time to slow it.",
                                     "That appears to be the job."),
                    Defeat = NP("The imperfect thing bursts before it can finish. Deeper down, a completed version of it feels the loss.",
                                "There is a finished one of you further down. I felt it flinch.")
                },
                ["CellSemiPerfect"] = new BossLines
                {
                    Meet = NSP("Taller now, almost handsome, and badly balanced on a body one component short of whole.",
                               "One part from complete, and that part is not in this mine and never will be. Do you understand what that is? To be permanently almost?",
                               "I am beginning to, yes."),
                    Rematch2 = SP("Still one short. Still you come.",
                                  "You have had forty days to make peace with it."),
                    Rematch3 = SP("Third time in this half-finished shape. I think you enjoy the incomplete.",
                                  "I think you are stuck. Same as everyone here. It is not personal."),
                    RematchLoop = SP("Semi-perfect is still more than you will ever be.",
                                     "And still not enough to get out."),
                    Defeat = NP("The half-finished form ruptures. Perfection is waiting a little further down.",
                                "Two of you now. There will be a third.")
                },
                ["CellJuniors"] = new BossLines
                {
                    Meet = NSP("A small blue thing drops out of the ceiling, all teeth, and giggles at you with its father's mouth.",
                               "Father made me to practise on things that scream. He is further down. He said to soften you up and watch how you move.",
                               "He is sending children ahead to take notes."),
                    Rematch2 = SP("I am bigger now. Practice does that.",
                                  "You are still a child running his errands."),
                    Rematch3 = SP("I have stopped screaming when I die. I learned that from you.",
                                  "That is not something to be proud of. I am sorry."),
                    RematchLoop = SP("I fight like you now. Father says that is the point of me.",
                                     "He is watching through you. Of course he is."),
                    Defeat = NP("The Junior pops. Somewhere below, the thing that fathered it is intrigued.",
                                "He has been studying me this whole time. Through a child.")
                },
                ["CellPerfect"] = new BossLines
                {
                    Meet = NSP("It steps out flawless and symmetrical, and it has very clearly been waiting for you specifically.",
                               "I have watched you through the small one. I know how you move, how you tire, which way you step when you are hurt. Perfect is not a boast. It is a method.",
                               "You built a child so you could watch me through its eyes."),
                    Rematch2 = SP("You unmade perfection once. It regenerated. It remembers. It improved.",
                                  "So did I. That is how this works now."),
                    Rematch3 = SP("Even perfect can be practised, it turns out.",
                                  "That is the first honest thing anyone has said to me down here."),
                    RematchLoop = SP("You are a flaw I keep having to correct.",
                                     "And yet here we both are. Again."),
                    Defeat = NSP("It regenerates once, twice, then not at all. Surviving something this composed teaches a stillness bluer than any rage.",
                                 "I am PERFECT, I cannot-",
                                 "You were. Down here that is just one more thing that does not get out.")
                },

                // ====================================================== floors 200-265: the turn
                ["Bojack"] = new BossLines
                {
                    Meet = NSP("Broken chain-links hang from his wrists. The shaft has been shifting for months, and something has finally worked loose.",
                               "I was not killed, farmer. I was sealed. And every wish you make on those spheres shakes these walls a little looser. You are the best thing that has happened to me in a thousand years.",
                               "...The wishes are doing that. Every wish I make opens this place up wider."),
                    Rematch2 = SP("The seal is weaker every time you come down. Keep wishing.",
                                  "I have noticed. I am being careful about it now."),
                    Rematch3 = SP("Third break-out. You are loosening my chains for me and you know it.",
                                  "I know. I have not decided what to do about that yet."),
                    RematchLoop = SP("The seal is a formality now. I stay in the dark because I enjoy leaving it.",
                                     "One day you will be right about that. Not today."),
                    Defeat = NSP("The chains reform around a falling shadow, and a reckless red art of overload stays behind with you.",
                                 "Back... in the dark...",
                                 "For now. I will think harder before the next wish.")
                },
                ["Broly"] = new BossLines
                {
                    Meet = NP("A giant fills the tunnel mouth, shaking, repeating one name under his breath. When he finally sees you, he stops saying it.",
                              "He is not looking for me. He is looking for someone who is not here, and I will do."),
                    Rematch2 = NP("The shaking is worse and the name is louder. There is less of the man each time and more of the noise.",
                                  "He is wearing out. Whatever is left of him is wearing out."),
                    Rematch3 = NP("He does not say the name any more. There is nothing left in him to say it with.",
                                  "I am sorry. I do not think you can hear that any more."),
                    RematchLoop = NP("The shape that used to be a man knows only that you are here.",
                                     "I know. Come on then."),
                    Defeat = NP("The endless roar finally, briefly, stops.",
                                "Rest. Just for a while. You have earned that much.")
                },
                ["Dabura"] = new BossLines
                {
                    Meet = NSP("A demon king in a very good cloak looks you over the way a builder prices a job.",
                               "I answer to the thing further down - the pink one, the hungry one. It felt you coming and it wants you softened first. I volunteered. Hell is dull and you are not.",
                               "There is something below you giving orders. Good. That narrows it down."),
                    Rematch2 = SP("Death is only a door, farmer. I keep walking back through it.",
                                  "Everyone down here does. It is the one thing this place is good at."),
                    Rematch3 = SP("Hell is dull. You are not.",
                                  "That is the nicest thing a demon has said to me."),
                    RematchLoop = SP("The Demon Realm spits me back up whenever you dig this deep. We are cursed together.",
                                     "Cursed together. Right. Let us get it over with."),
                    Defeat = NSP("Dabura crumbles to stone, cursing the master who spent him.",
                                 "The Majin will swallow you whole-",
                                 "Then I had better go and meet it.")
                },
                ["BuuFat"] = new BossLines
                {
                    Meet = NSP("Something round and pink bounces once and grins at you with no malice at all, which is somehow worse.",
                               "Ooh! You not rock! You move! Buu been alone in the dark so long. You play with Buu now?",
                               "...I do not think it knows what it is. That is the worst thing I have seen down here."),
                    Rematch2 = SP("You hurt Buu. Buu remember hurt now.",
                                  "I am sorry. There is no way past you that is not through you."),
                    Rematch3 = SP("Third time you make Buu angry. Angry Buu is a different Buu.",
                                  "I know. I have met him. He is further down."),
                    RematchLoop = SP("Buu always come back. Buu cannot be candy. Only you.",
                                     "Come on then. Gently, if we can."),
                    Defeat = NP("Fat Buu deflates with a childish wail - and the anger that was inside it does not die with it.",
                                "It split. The angry part went deeper. I felt it go.")
                },
                ["SuperBuu"] = new BossLines
                {
                    Meet = NSP("A leaner, crueller pink shape tilts its head - the temper that walked out of the round one on the floor above.",
                               "I came out of the fat one when it finally got angry, and I have been getting hungrier ever since. You smell strong. When I eat you I will move like you.",
                               "That is why you are all so interested in me. I am the only living thing down here."),
                    Rematch2 = SP("I ate stronger things than last time. Are you more than last time?",
                                  "Yes. Unfortunately for both of us."),
                    Rematch3 = SP("Still will not hold still! I will wear you OUT.",
                                  "You keep saying that. You keep not doing it."),
                    RematchLoop = SP("You always come back tasty.",
                                     "And you always come back hungry. Here we are."),
                    Defeat = NP("Super Buu deflates with a long, disappointed sigh, having absorbed nothing at all.",
                                "There is a quieter one below this. I would rather there was not.")
                },
                ["BuuSuperGohan"] = new BossLines
                {
                    Meet = NSP("Leaner still, and terribly quiet. This one ate something that could think, and it made it patient.",
                               "The round one raged. The hungry one copied. I ate better than either and it made me still. I do not need to rage at you. I can wait as long as this mine lasts.",
                               "That is the difference down here. None of you can leave, so the patient ones win."),
                    Rematch2 = SP("The others raged and lost. I wait, and I remember you.",
                                  "I remember you too. That is the trouble with the quiet ones."),
                    Rematch3 = SP("You have earned a stillness I keep for gods.",
                                  "There is one of those at the bottom. I have been told."),
                    RematchLoop = SP("I have eaten better than you and grown quiet. Your return is the only thing that stirs me.",
                                     "Then let us not keep each other waiting."),
                    Defeat = NSP("The absorbed calm shatters into a shriek. Surviving something this composed teaches the body to move on its own.",
                                 "This is not how it ends for me-",
                                 "It never is. Forty days. I will be here.")
                },
                ["MetalCoolerLegion"] = new BossLines
                {
                    Meet = NSP("Cooler's face again, in cold chrome this time, and the walls behind it are stacked with half-built copies of it.",
                               "You broke both my living forms above. The machine down here kept the pattern regardless. Break this body and it prints another. That is the advantage of not being properly alive.",
                               "There is a machine down here now. Feeding on the same wound as everything else."),
                    Rematch2 = SP("The pattern has been improved. You will not enjoy the improvements.",
                                  "You said that as a person, too."),
                    Rematch3 = SP("Version three. Every scratch you left has been studied.",
                                  "Then you already know how this goes."),
                    RematchLoop = SP("Break one, meet the next. Forever.",
                                     "Forever is a long time to spend being a copy of your brother."),
                    Defeat = NP("The chrome seizes and dies. The pattern has, for the moment, run out of copies.",
                                "Even the machine cannot get out of here. Nothing can.")
                },
                ["KidBuu"] = new BossLines
                {
                    Meet = NP("Small, pink, grinning at nothing in particular. This is the oldest thing in the shaft: what Buu was before it learned to want anything at all.",
                              "It does not want the wish. It does not want anything. That is worse than all of them."),
                    Rematch2 = NP("It reformed out of nothing, the way it always does. It does not remember you.",
                                  "It never will. Every time is the first time, for it."),
                    Rematch3 = NP("It has forgotten you utterly. To it, this is always the first meeting.",
                                  "I would rather be hated. At least being hated is a kind of company."),
                    RematchLoop = NP("Pink, grinning, blank. It destroys because destroying is what it is.",
                                     "Nothing to say to you. There is no one in there to say it to."),
                    Defeat = NP("Kid Buu comes apart in a giggle and does not, this time, put itself back together.",
                                "That one was never a person. That one was the hole itself, wearing a shape.")
                },

                // ====================================================== floors 280-300: the bottom
                ["FriezaGolden"] = new BossLines
                {
                    Meet = NSP("Gold light in the dark, and a familiar cruelty that has very obviously spent every one of its deaths training.",
                               "You met me weak in the shallows and thought that was me. Do you know what I have done with being dead, farmer? I trained. Every single time you killed me, I came back and I trained.",
                               "That is what the rest of them should have been doing."),
                    Rematch2 = SP("You put down my golden form. Few can say that. None say it twice.",
                                  "We will find out."),
                    Rematch3 = SP("I have learned to hold the gold longer. Stamina was always my one flaw.",
                                  "You are the only one down here who has actually got better on purpose."),
                    RematchLoop = SP("Down here I have all the time in the world to keep meeting you.",
                                     "So do I. That is exactly the trouble."),
                    Defeat = NSP("Golden Frieza falls, certain even as he falls that there is a shade past gold still waiting below.",
                                 "This is not the end of me. There is a colour past gold-",
                                 "I know. I am going down to meet it.")
                },
                ["FriezaBlack"] = new BossLines
                {
                    Meet = NSP("No aura at all. Just black, and quiet, and entirely certain.",
                               "First form. Final form. Gold. You have beaten every shape I own, and each time I went back into the dark and worked. This one I did not train for power. I trained it for you, specifically.",
                               "You spent a hundred deaths on me. I am not sure whether that is flattering."),
                    Rematch2 = SP("You beat black. Then I was not black enough.",
                                  "You will say that again next time, too."),
                    Rematch3 = SP("Three times against the form that surpassed everyone. You are the exception I keep making.",
                                  "I am the only thing down here that changes. That is all I am."),
                    RematchLoop = SP("There is nothing past this. Only me, again, and worse.",
                                     "Then we will do this again. And again."),
                    Defeat = NP("Black Frieza goes still without a sound. Far below, on a heap of fallen stone, something that had been asleep opens one eye.",
                                "...Something heard that. Something all the way at the bottom.")
                },
                ["Destroyer"] = new BossLines
                {
                    Meet = NSP("The shaft ends. At the bottom, on a heap of fallen stone, a lean figure is asleep, and has been for a very long time.",
                               "Mm. You woke me. Do you know what I am? I am the one who was supposed to come down here and erase this - the hole, the leak, all of it - before it started letting the dead back in. I decided watching was more interesting.",
                               "You let this happen. Every one of them up there is your fault."),
                    Rematch2 = SP("Back? I spared you last time out of curiosity. Do not rely on it twice.",
                                  "You could close this hole. You simply do not want to."),
                    Rematch3 = SP("Three visits. I am beginning to think of you as a pet.",
                                  "Close it. You could close it today and you are bored instead."),
                    RematchLoop = SP("Ah. My recurring little insult. Sit. Fight. Amuse me.",
                                     "One day you will be interested enough to do your job."),
                    Defeat = NSP("For the first time in an age the god chooses to sit back down rather than end everything.",
                                 "Interesting. Live, then. For now.",
                                 "You still will not close it, will you. So it stays mine to hold.")
                },

                // ====================================================== the seven spheres
                ["BallGuardian1"] = new BossLines
                {
                    Meet = NP("A sphere the size of a fist sits in a hollow in the rock, glowing orange, one star suspended inside it. Something enormous has grown around it like scar tissue.",
                              "One star. So there are others, further down. That is what everything here is circling."),
                    Rematch2 = NP("The sphere drifted back into its hollow, and the dark grew a new keeper around it.",
                                  "It comes back. Of course it comes back. Everything here does."),
                    Rematch3 = NP("The keeper has grown extra coils in your absence, all of them meant for you.",
                                  "It has been learning from me while I was away."),
                    RematchLoop = NP("The guardian no longer guards the sphere so much as waits for you to come and take it.",
                                     "Right. Again, then."),
                    Defeat = NP("The keeper shatters and the One-Star Ball rolls free, warm in your hand.",
                                "Seven of these makes a wish. That is what every dead thing down here wants.")
                },
                ["BallGuardian2"] = new BossLines
                {
                    Meet = NP("A great serpent has wound itself around the Two-Star Ball, and something older rides in its head: a borrowed memory of light gathered out of every living thing.",
                              "Two. The spheres hold the hole open - that is why they are down here and not up there."),
                    Rematch2 = NP("The sphere is back in the coils. The guardian has not forgotten what you took.",
                                  "Neither have I. Sorry."),
                    Rematch3 = NP("The borrowed light it carries has learned to strike first.",
                                  "It is using what it took from me. Everything down here does that eventually."),
                    RematchLoop = NP("The serpent barely stirs. It simply waits for you to feed it your ki again.",
                                     "Not today."),
                    Defeat = NP("The serpent uncoils in death, and the Two-Star Ball - and the trick of gathering light out of every living thing - are yours.",
                                "Two. Five to go.")
                },
                ["BallGuardian3"] = new BossLines
                {
                    Meet = NP("Three stars in the glass, and a keeper that has not slept since the sphere fell here.",
                              "Three. They are heavier the deeper they sit. I can feel this one pulling."),
                    Rematch2 = NP("The sphere returned; so did its watcher, angrier for the wait.",
                                  "Forty days, like clockwork. This place runs on a season."),
                    Rematch3 = NP("It does not blink any more. It only watches the ladder.",
                                  "It is waiting for me specifically now. That is new."),
                    RematchLoop = NP("Sleepless, endless, and yours to put down again.",
                                     "Sorry. I need what you are sitting on."),
                    Defeat = NP("The keeper falls and the Three-Star Ball is loosed from the dark.",
                                "Three.")
                },
                ["BallGuardian4"] = new BossLines
                {
                    Meet = NP("Four stars. Somewhere, once, this particular sphere meant grandfather to somebody.",
                              "Four. Someone loved this one, a long way from here. You can feel it in the glass."),
                    Rematch2 = NP("The coils have reformed around the sphere, tighter than before.",
                                  "It does not want to give this one up. I understand that."),
                    Rematch3 = NP("The keeper strikes where you will be, not where you are.",
                                  "It has been practising on the memory of me."),
                    RematchLoop = NP("The four-starred sphere returns to its coils each time. So do you.",
                                     "So we do."),
                    Defeat = NP("The keeper falls away and the Four-Star Ball, warm as a memory, is yours.",
                                "Four. Three left.")
                },
                ["BallGuardian5"] = new BossLines
                {
                    Meet = NP("Five stars behind a keeper that fills the tunnel wall to wall. There is no way around it.",
                              "Five. And the deeper ones are guarded harder. Whatever wants these kept knows I am close."),
                    Rematch2 = NP("It fills the shaft again, and this time it was expecting you.",
                                  "It knew the day I would come. They all do now."),
                    Rematch3 = NP("There is no seam to slip through. Only forward.",
                                  "Only forward. Fine."),
                    RematchLoop = NP("The wall reforms across the shaft each time you take the sphere.",
                                     "Then I will take it again."),
                    Defeat = NP("The wall comes down and the Five-Star Ball rolls into the light.",
                                "Five.")
                },
                ["BallGuardian6"] = new BossLines
                {
                    Meet = NP("Six stars at the centre of a slow, endless circling, and the dust never quite settles.",
                              "Six. One more after this, and then I have to decide what a wish is worth."),
                    Rematch2 = NP("The procession resumed the moment you left.",
                                  "It never actually stopped, did it."),
                    Rematch3 = NP("The circling has quickened. The dust does not settle even between your visits.",
                                  "It is getting worse the closer I get to the bottom."),
                    RematchLoop = NP("Round and round, as endless as your returns.",
                                     "Round we go."),
                    Defeat = NP("The procession halts. The Six-Star Ball lies still at its centre.",
                                "Six. One left.")
                },
                ["BallGuardian7"] = new BossLines
                {
                    Meet = NP("The last sphere, seven stars, and the dark has put everything it has left around it. With this one the set is whole - and a whole set is a wish.",
                              "Seven. Everything down here has been waiting a very long time for somebody to manage this."),
                    Rematch2 = NP("The seventh is the hardest to keep and the hardest to take. Its guard remembers your face.",
                                  "It should. We have done this before."),
                    Rematch3 = NP("The guard has grown. The dark does not want to lose this one twice.",
                                  "It knows what happens when I get all seven. So do I, now."),
                    RematchLoop = NP("The final sphere always returns to the deepest, angriest guard.",
                                     "And I always come back for it."),
                    Defeat = NP("The guard falls in a tangle of shadow and scale. The Seven-Star Ball is yours - and with it, the wish.",
                                "Seven. Every wish opens this place wider. I know that now, and I am going to make one anyway.")
                }
            };
    }
}
