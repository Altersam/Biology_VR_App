using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace BiologyVR.V2.Editor
{
    /// <summary>
    /// Publishes the prepared app-repository kit (Tools/prepare_bvr2_release.py must have run):
    /// commit + push of the kit (sources, README, VERSION.json; APKs stay gitignored) and a GitHub
    /// release with both APKs attached, through the user's own git credentials and the project's
    /// gh wrapper (Tools/github_cli.py). Runs in the background; progress in
    /// Reports/GitHubPublish_0_11_0.json. Refuses on an unexpected remote, a diverged branch or
    /// forbidden files in the commit.
    /// </summary>
    public static class Bvr2GitHubPublish
    {
        const string RepoSlug = "Altersam/Biology_VR_App";
        static readonly string Tag = "v" + Bvr2AndroidBuild.Version;
        static readonly string ReportPath = Path.GetFullPath(Bvr2SceneBuilder.Root + "/Reports/GitHubPublish_0_11_0.json");
        static bool running;

        [MenuItem("Biology VR 2/Android/Publish GitHub Release", false, 220)]
        static void Publish()
        {
            if (running) throw new InvalidOperationException("Publishing is already running");
            string project = Path.GetFullPath(".");
            string repo = Path.GetFullPath(Path.Combine(project, "..", "Github_VR", "Biology_VR_App"));
            if (!Directory.Exists(Path.Combine(repo, ".git"))) throw new InvalidOperationException("App repository kit not found next to the project (open the project at its original path): " + repo);
            running = true;
            Task.Run(() => Run(project, repo));
        }

        sealed class Step { public string name; public int exit; public string output; }

        static void Run(string project, string repo)
        {
            var steps = new List<Step>(); string result = "Running", reason = null;
            void Save() => File.WriteAllText(ReportPath, JsonConvert.SerializeObject(new { utc = DateTime.UtcNow, tag = Tag, repository = RepoSlug, result, reason, steps, headsetVerified = false }, Formatting.Indented));
            Step Do(string name, string exe, string args, string cwd, bool mustSucceed = true)
            {
                var s = Exec(exe, args, cwd); s.name = name; steps.Add(s); Save();
                if (mustSucceed && s.exit != 0) throw new InvalidOperationException($"{name} failed (exit {s.exit})");
                return s;
            }
            try
            {
                Save();
                string remote = Do("remote", "git", "remote get-url origin", repo).output.Trim().TrimEnd('/');
                if (!remote.Equals("https://github.com/" + RepoSlug, StringComparison.OrdinalIgnoreCase) && !remote.Equals("https://github.com/" + RepoSlug + ".git", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Unexpected remote " + remote);
                Do("fetch", "git", "fetch origin main", repo);
                string head = Do("head", "git", "rev-parse HEAD", repo).output.Trim();
                string upstream = Do("origin/main", "git", "rev-parse origin/main", repo).output.Trim();
                string ahead = Do("ahead", "git", "rev-list --count origin/main..HEAD", repo).output.Trim();
                if (head != upstream && ahead == "0") throw new InvalidOperationException("Local kit is behind origin/main — pull first");
                Do("add", "git", "add -A", repo);
                var staged = Do("staged", "git", "diff --cached --name-only", repo).output.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
                var forbidden = staged.Where(f => Regex.IsMatch(f, @"\.(apk|aab|jks|keystore)$", RegexOptions.IgnoreCase) || f.EndsWith("McpUnitySettings.json") || f.Contains("bridge-token") || f.StartsWith("ReleaseFiles/")).ToList();
                if (forbidden.Count > 0) { Do("unstage", "git", "reset -q", repo, false); throw new InvalidOperationException("Forbidden files staged: " + string.Join(", ", forbidden.Take(10))); }
                if (staged.Count > 0)
                {
                    string message = Path.Combine(Path.GetTempPath(), "bvr2_commit_message.txt");
                    File.WriteAllText(message, $"Add Biology VR 2 {Bvr2AndroidBuild.Version} preview: sources and headset builds\n\n" +
                        "New game Biology VR 2 (lab prologue + six chapters) as a separate app com.biologyvr.biologyvr2,\n" +
                        $"PICO 4 and Meta Quest APKs {Bvr2AndroidBuild.Version} (versionCode {Bvr2AndroidBuild.AndroidCode}) attached to release {Tag}.\n" +
                        "Editor autoplay of prologue and chapters 1-6 passed; not yet verified on a headset.\n\n" +
                        "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>\n" +
                        "Claude-Session: https://claude.ai/code/session_01MTHkqtqiy7lgch7nzbeqr8\n", new UTF8Encoding(false));
                    Do("commit", "git", $"commit -q -F \"{message}\"", repo);
                }
                Do("push", "git", "push origin HEAD:main", repo);
                string python = Exec("python", "--version", project).exit == 0 ? "python" : "py";
                string assets = $"\"ReleaseFiles/BiologyVR2_Pico4_v{Bvr2AndroidBuild.Version}.apk\" \"ReleaseFiles/BiologyVR2_MetaQuest_v{Bvr2AndroidBuild.Version}.apk\" \"ReleaseFiles/SHA256SUMS_0_11_0.txt\"";
                var exists = Do("release-exists", python, $"Tools/github_cli.py release view {Tag} --repo {RepoSlug} --json tagName", project, false);
                if (exists.exit != 0)
                    Do("release", python, $"Tools/github_cli.py release create {Tag} {assets} --repo {RepoSlug} --target main --prerelease --title \"Biology VR 2 — {Bvr2AndroidBuild.Version} (предпросмотр)\" --notes-file \"unity/BiologyVR/RELEASE_0_11_0_BVR2_RU.md\"", project);
                else
                    Do("upload", python, $"Tools/github_cli.py release upload {Tag} {assets} --repo {RepoSlug} --clobber", project);
                Do("verify", python, $"Tools/github_cli.py release view {Tag} --repo {RepoSlug} --json url,isPrerelease,tagName,assets", project);
                result = "Succeeded";
            }
            catch (Exception e) { result = "Failed"; reason = e.Message; }
            finally { running = false; Save(); Debug.Log($"[BVR2] GitHub publish {result}{(reason != null ? ": " + reason : "")}"); }
        }

        static Step Exec(string exe, string args, string cwd)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    WorkingDirectory = cwd, UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
                };
                psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
                psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
                using var p = Process.Start(psi);
                var err = p.StandardError.ReadToEndAsync();
                string output = p.StandardOutput.ReadToEnd();
                if (!p.WaitForExit(30 * 60 * 1000)) { try { p.Kill(); } catch { } return new Step { exit = -2, output = "timeout" }; }
                string all = (output + err.Result).Trim();
                all = Regex.Replace(all, @"(gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,})", "***");
                return new Step { exit = p.ExitCode, output = all.Length > 4000 ? all.Substring(0, 4000) + "…" : all };
            }
            catch (Exception e) { return new Step { exit = -1, output = e.Message }; }
        }
    }
}
