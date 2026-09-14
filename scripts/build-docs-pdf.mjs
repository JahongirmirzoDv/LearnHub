// Builds the LearnHub documentation as PDF:
//   * one combined document (full-bleed cover + contents with real page numbers + every document)
//   * one PDF per document
// Pipeline: Markdown -> HTML (marked) -> rendered Mermaid -> PDF (headless Chromium) -> merged (pdf-lib).
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { marked } from "marked";
import { gfmHeadingId } from "marked-gfm-heading-id";
import { chromium } from "playwright";
import { PDFDocument } from "pdf-lib";

/** Reads the text of every page, so the contents can quote the real page numbers. */
async function pageTexts(file) {
  const pdfjs = await import("pdfjs-dist/legacy/build/pdf.mjs");
  const doc = await pdfjs.getDocument({ data: new Uint8Array(fs.readFileSync(file)), useSystemFonts: true }).promise;
  const out = [];
  for (let p = 1; p <= doc.numPages; p++) {
    const page = await doc.getPage(p);
    const content = await page.getTextContent();
    out.push(content.items.map((i) => i.str).join(" ").replace(/\s+/g, " "));
  }
  return out;
}

const HERE = path.dirname(fileURLToPath(import.meta.url));
const REPO = path.resolve(HERE, "..");
const DOCS = path.join(REPO, "docs");
const OUT = path.join(DOCS, "pdf");
const TMP = path.join(os.tmpdir(), "learnhub-docs-pdf");
const FONT = path.join(REPO, "FirebaseLanding", "assets", "fonts", "overpass-latin-wght-normal.woff2");

const ORDER = [
  ["PROPOSAL", "Proposal"],
  ["REQUIREMENT_TRACEABILITY", "Requirement Traceability"],
  ["REQUIREMENTS_CHECKLIST", "Requirements Checklist"],
  ["USE_CASES", "Use Cases"],
  ["NAVIGATION", "Navigation Structure"],
  ["WIREFRAMES", "Wireframes"],
  ["FLOWCHARTS", "Flowcharts"],
  ["ARCHITECTURE", "Architecture"],
  ["DATABASE", "Database Design"],
  ["ERD", "Entity Relationship Diagram"],
  ["SECURITY", "Security Review"],
  ["TESTING", "Testing"],
  ["DEPLOYMENT", "Deployment"],
  ["USER_GUIDE", "User Guide"],
  ["REPORT_NOTES", "Report Notes"],
  ["GIT_WORKFLOW", "Git Workflow"],
  ["TEAM", "Team and Responsibilities"],
  ["VIVA", "Viva Preparation"],
];

marked.use({ gfm: true, breaks: false });
marked.use(gfmHeadingId());

const slug = (s) => s.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");

const MIME = { ".png": "image/png", ".jpg": "image/jpeg", ".jpeg": "image/jpeg", ".gif": "image/gif",
               ".svg": "image/svg+xml", ".webp": "image/webp" };

function fixLinks(html) {
  // Images are inlined as data URIs. A file:// subresource does not load when the document is set with
  // setContent, because the page origin is about:blank rather than file://, and the screenshots then print
  // as broken-image placeholders.
  html = html.replace(/(<img[^>]+src=")([^"]+)(")/g, (m, a, src, c) => {
    if (/^(https?:|data:)/.test(src)) return m;
    const abs = path.resolve(DOCS, src);
    const ext = path.extname(abs).toLowerCase();
    if (!fs.existsSync(abs) || !MIME[ext]) { console.warn("      missing image:", src); return m; }
    return a + `data:${MIME[ext]};base64,` + fs.readFileSync(abs).toString("base64") + c;
  });
  html = html.replace(/(<a[^>]+href=")([^"]+\.md)(#[^"]*)?(")/g, (m, a, href, hash, c) => {
    const base = path.basename(href, ".md");
    return a + "#" + slug(base) + (hash || "") + c;
  });
  return html;
}

const prepareMermaid = (html) =>
  html.replace(/<pre><code class="language-mermaid">([\s\S]*?)<\/code><\/pre>/g, (m, code) =>
    '<div class="mermaid">' + code.replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&amp;/g, "&") + "</div>");

function readDoc(name) {
  const file = path.join(DOCS, name + ".md");
  if (!fs.existsSync(file)) return null;
  return prepareMermaid(marked.parse(fs.readFileSync(file, "utf8").replace(/^#\s+.*\n/, "")));
}

const CSS = `
@font-face { font-family:"Overpass"; font-weight:100 900; font-style:normal;
  src:url("file://${FONT}") format("woff2"); }
:root{--ink:#1b2438;--muted:#5b6577;--line:#d9dee7;--red:#c9344a;--teal:#157f8e;--blue:#2865c9;--bg:#f6f7f9;}
@page { size:A4; }
*{box-sizing:border-box;}
html{-webkit-print-color-adjust:exact;print-color-adjust:exact;}
body{font-family:"Overpass",-apple-system,"Segoe UI",Roboto,sans-serif;font-size:10.5pt;line-height:1.55;
     color:var(--ink);margin:0;}
h1,h2,h3,h4{line-height:1.25;break-after:avoid-page;}
h1{font-size:19pt;margin:0 0 6mm;padding-bottom:3mm;border-bottom:2.5pt solid var(--red);}
h2{font-size:14pt;margin:8mm 0 3mm;}
h3{font-size:11.5pt;margin:6mm 0 2mm;color:var(--teal);}
h4{font-size:10.5pt;margin:4mm 0 1.5mm;color:var(--muted);text-transform:uppercase;letter-spacing:.04em;}
p{margin:0 0 3mm;} a{color:var(--blue);text-decoration:none;} strong{font-weight:650;}
hr{border:0;border-top:1px solid var(--line);margin:7mm 0;}
ul,ol{margin:0 0 3mm;padding-left:6mm;} li{margin-bottom:1.2mm;} li>ul,li>ol{margin-top:1.2mm;}
code{font-family:"SF Mono",Menlo,Consolas,monospace;font-size:8.8pt;background:var(--bg);
     border:1px solid var(--line);border-radius:3px;padding:.5mm 1.2mm;}
pre{background:var(--bg);border:1px solid var(--line);border-left:3px solid var(--teal);border-radius:4px;
    padding:3mm;white-space:pre-wrap;word-break:break-word;break-inside:avoid-page;}
pre code{background:none;border:0;padding:0;font-size:8.4pt;}
table{width:100%;border-collapse:collapse;margin:0 0 4mm;font-size:9.3pt;}
thead{background:var(--ink);color:#fff;}
th,td{border:1px solid var(--line);padding:1.6mm 2mm;text-align:left;vertical-align:top;}
th{font-weight:600;} tbody tr:nth-child(even){background:var(--bg);} tr{break-inside:avoid-page;}
blockquote{margin:0 0 4mm;padding:2.5mm 3.5mm;background:#fff8e6;border-left:3px solid #ffc53d;
           border-radius:0 4px 4px 0;} blockquote p:last-child{margin-bottom:0;}
img{max-width:100%;height:auto;border:1px solid var(--line);border-radius:4px;}
.mermaid{text-align:center;margin:4mm 0;break-inside:avoid-page;}
/* Mermaid sizes its SVG at its natural width, and a tall flowchart would then run past the page and be split
   mid-figure. Bounding both dimensions with the viewBox in place scales it down to fit one page instead. */
.mermaid svg{width:auto !important;height:auto !important;max-width:100% !important;max-height:228mm !important;}

/* Full-bleed cover, rendered as its own PDF with no margins so the panel fills the sheet. */
.cover{width:210mm;height:297mm;padding:30mm 20mm 20mm;background:var(--ink);color:#fff;
       display:flex;flex-direction:column;font-family:"Overpass",sans-serif;}
.cover .brand{display:flex;align-items:center;gap:4mm;font-size:15pt;font-weight:600;}
.cover .dot{width:9mm;height:9mm;border-radius:50%;background:#fff;color:var(--ink);
            display:flex;align-items:center;justify-content:center;font-weight:700;font-size:11pt;}
.cover h1{border:0;font-size:34pt;margin:22mm 0 0;padding:0;color:#fff;line-height:1.15;}
.cover .rule{height:3px;width:40mm;background:var(--red);margin:6mm 0 8mm;}
.cover .sub{font-size:15pt;color:#ffc53d;margin:0 0 14mm;}
.cover .lead{font-size:11pt;color:#c3cad8;max-width:132mm;}
.cover dl{margin-top:auto;display:grid;grid-template-columns:42mm 1fr;gap:2.5mm 5mm;font-size:10pt;}
.cover dt{color:#8f9bb0;} .cover dd{margin:0;}

.toc{page-break-after:always;}
.toc h1{border-bottom-color:var(--teal);}
.toc ol{list-style:none;padding:0;}
.toc li{display:flex;align-items:baseline;gap:2mm;margin-bottom:1.8mm;font-size:11pt;}
.toc .dots{flex:1;border-bottom:1px dotted #b9c1cf;transform:translateY(-1mm);}
.toc .pg{color:var(--muted);font-variant-numeric:tabular-nums;}
.doc{page-break-before:always;}
.doc-title{font-size:23pt;border-bottom:2.5pt solid var(--red);padding-bottom:3mm;margin:0 0 7mm;}
/* Invisible marker so the build can find where each document starts by reading the PDF back.
   Searching for the visible title is unreliable: the documents cross-reference each other by name. */
.anchor{font-size:1pt;color:#ffffff;}
`;

const page = (body, extraCss = "") =>
  `<!doctype html><html lang="en"><head><meta charset="utf-8"><style>${CSS}${extraCss}</style></head><body>${body}</body></html>`;

/** Renders Mermaid in the page and returns it, ready for printing. */
async function prepare(browser, html) {
  const p = await browser.newPage();
  await p.setContent(html, { waitUntil: "networkidle", timeout: 120000 });
  const count = await p.locator(".mermaid").count();
  if (count > 0) {
    await p.addScriptTag({ url: "https://cdn.jsdelivr.net/npm/mermaid@11.17.2/dist/mermaid.min.js" });
    await p.evaluate(() => window.mermaid.initialize({
      startOnLoad: false, securityLevel: "loose", theme: "base", fontFamily: "Overpass, sans-serif",
      themeVariables: { primaryColor: "#ffffff", primaryBorderColor: "#1b2438", primaryTextColor: "#1b2438",
                        lineColor: "#566076", fontSize: "14px" },
    }));
    const failed = await p.evaluate(async () => {
      const bad = [];
      for (const [i, n] of Array.from(document.querySelectorAll(".mermaid")).entries()) {
        try {
          const { svg } = await window.mermaid.render("m" + i, n.textContent);
          n.innerHTML = svg;
        } catch (e) { bad.push(i + ": " + e.message); n.innerHTML = "<pre>" + n.textContent + "</pre>"; }
      }
      return bad;
    });
    if (failed.length) console.warn("      mermaid failures:", failed);
    await p.waitForTimeout(500);
  }
  return p;
}

async function toPdf(p, file, { header, footer, margin }) {
  await p.pdf({
    path: file, format: "A4", printBackground: true,
    displayHeaderFooter: Boolean(header || footer),
    headerTemplate: header || "<div></div>",
    footerTemplate: footer || "<div></div>",
    margin: margin || { top: "18mm", bottom: "16mm", left: "16mm", right: "16mm" },
  });
  await p.close();
  const doc = await PDFDocument.load(fs.readFileSync(file));
  return doc.getPageCount();
}

const HDR = (right) => `<div style="font:8pt 'Overpass',sans-serif;color:#8f9bb0;width:100%;padding:0 16mm;
  display:flex;justify-content:space-between;"><span>LearnHub &middot; Web-Based Learning System</span>
  <span>${right}</span></div>`;
const FTR = `<div style="font:8pt 'Overpass',sans-serif;color:#8f9bb0;width:100%;padding:0 16mm;
  display:flex;justify-content:space-between;"><span>CT050-3-2-WAPP group assignment</span>
  <span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span></div>`;

function coverHtml() {
  const today = new Date().toLocaleDateString("en-GB", { day: "numeric", month: "long", year: "numeric" });
  return `<div class="cover">
  <div class="brand"><span class="dot">L</span> LearnHub</div>
  <h1>Web-Based<br>Learning System</h1>
  <div class="rule"></div>
  <p class="sub">Project Documentation</p>
  <p class="lead">A complete ASP.NET Core MVC learning management system: course catalogue, lessons, quizzes and
  progress tracking for students, with a full administration area. Deployed on Railway with a Firebase Hosting
  presentation site.</p>
  <dl>
    <dt>Author</dt><dd>Khaytboy Khayrullaev (TP072305)</dd>
    <dt>Module</dt><dd>CT050-3-2-WAPP Web Applications</dd>
    <dt>Institution</dt><dd>Asia Pacific University of Technology &amp; Innovation</dd>
    <dt>Assessment</dt><dd>Group assignment, carried out individually</dd>
    <dt>Live application</dt><dd>learnhub-production-7081.up.railway.app</dd>
    <dt>Project website</dt><dd>learnhub-wapp.web.app</dd>
    <dt>Repository</dt><dd>github.com/KhayitOff/LearnHub</dd>
    <dt>Generated</dt><dd>${today}</dd>
  </dl></div>`;
}

function tocHtml(startPages) {
  const items = ORDER.map(([name, title], i) => {
    const pg = startPages[i];
    return `<li><span>${title}</span><span class="dots"></span><span class="pg">${pg ?? ""}</span></li>`;
  }).join("");
  return `<section class="toc"><h1>Contents</h1><ol>${items}</ol></section>`;
}

(async () => {
  fs.mkdirSync(OUT, { recursive: true });
  fs.mkdirSync(TMP, { recursive: true });
  const browser = await chromium.launch();

  // ---- one PDF per document (no forced break: a standalone file needs no leading blank leaf) ----
  for (const [name, title] of ORDER) {
    const body = readDoc(name);
    if (body === null) continue;
    const p = await prepare(browser, page(`<section><h1 class="doc-title">${title}</h1>${fixLinks(body)}</section>`));
    const n = await toPdf(p, path.join(OUT, name + ".pdf"), { header: HDR(title), footer: FTR });
    console.log(`  ${name.padEnd(28)} ${String(n).padStart(3)} pages`);
  }

  const bodyHtml = (numbers) => page([
    tocHtml(numbers),
    ...ORDER.map(([name, title]) => {
      const body = readDoc(name);
      return body === null ? "" :
        `<section class="doc" id="${slug(name)}"><h1 class="doc-title">${title}` +
        `<span class="anchor">@@DOCSTART-${name}@@</span></h1>${fixLinks(body)}</section>`;
    }),
  ].join(""));

  // ---- pass 1: render with blank page numbers, then read back where each document really begins.
  //      Measuring the combined layout is the only reliable way: a document's page count on its own
  //      differs from the space it takes inside the bundle.
  const probeFile = path.join(TMP, "body-probe.pdf");
  const probe = await prepare(browser, bodyHtml(ORDER.map(() => "")));
  const probePages = await toPdf(probe, probeFile, { header: HDR("Complete documentation"), footer: FTR });

  const texts = await pageTexts(probeFile);
  const startPages = ORDER.map(([name]) => {
    const i = texts.findIndex((t) => t.includes(`@@DOCSTART-${name}@@`));
    return i >= 0 ? i + 1 : "";
  });
  const missing = ORDER.filter((_, i) => startPages[i] === "");
  if (missing.length) console.warn("  warning: no start page found for", missing.map(([n]) => n).join(", "));
  console.log(`\n  first document starts on body page ${startPages[0]}; last on ${startPages[startPages.length - 1]}`);

  // ---- pass 2: the real body, now carrying the measured page numbers ----
  const bodyFile = path.join(TMP, "body.pdf");
  const bodyPage = await prepare(browser, bodyHtml(startPages));
  const bodyPages = await toPdf(bodyPage, bodyFile, { header: HDR("Complete documentation"), footer: FTR });
  if (bodyPages !== probePages) console.warn(`  warning: pagination moved between passes (${probePages} -> ${bodyPages})`);

  // ---- full-bleed cover: no margins, no header or footer, merged in front ----
  const coverPage = await prepare(browser, page(coverHtml()));
  const coverFile = path.join(TMP, "cover.pdf");
  await toPdf(coverPage, coverFile, { margin: { top: "0", bottom: "0", left: "0", right: "0" } });

  const merged = await PDFDocument.create();
  for (const f of [coverFile, bodyFile]) {
    const src = await PDFDocument.load(fs.readFileSync(f));
    for (const pg of await merged.copyPages(src, src.getPageIndices())) merged.addPage(pg);
  }
  merged.setTitle("LearnHub - Web-Based Learning System: Project Documentation");
  merged.setAuthor("CT050-3-2-WAPP group project");
  merged.setSubject("ASP.NET Core MVC learning management system - project documentation");
  merged.setProducer("LearnHub documentation build");
  const combinedFile = path.join(OUT, "LearnHub-Documentation.pdf");
  fs.writeFileSync(combinedFile, await merged.save());

  await browser.close();
  const total = await PDFDocument.load(fs.readFileSync(combinedFile));
  console.log(`\n  LearnHub-Documentation.pdf   ${total.getPageCount()} pages (cover + ${bodyPages} numbered)`);
})();

