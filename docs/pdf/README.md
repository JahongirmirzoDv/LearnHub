# LearnHub documentation (PDF)

Ready-to-submit PDFs generated from the Markdown in `docs/`.

| File | Contents |
|---|---|
| `LearnHub-Documentation.pdf` | Everything in one document: cover, contents with page numbers, then all eighteen documents |
| `<NAME>.pdf` | One PDF per document, for handing in a single section on its own |

## Regenerating

The PDFs are generated from the Markdown, so edit `docs/*.md` and rebuild rather than editing a PDF.

```bash
npm install --no-save marked marked-gfm-heading-id playwright pdf-lib
npx playwright install chromium
node scripts/build-docs-pdf.mjs
```

The script:

1. converts each `docs/*.md` with **marked** (GitHub-flavoured Markdown, so tables and task lists work);
2. renders every Mermaid diagram by loading the page in headless **Chromium** and running Mermaid from its CDN,
   so the architecture, ERD and flowchart diagrams appear as diagrams rather than code;
3. inlines the screenshots as data URIs — a `file://` image does not load inside a page set with `setContent`;
4. prints A4 with a running header, a footer with "Page N of M" and the styled tables, callouts and code blocks;
5. measures where each document actually starts **by reading the rendered PDF back**, then re-renders the contents
   with those page numbers. Measuring is necessary: a document's page count on its own is not the space it takes
   inside the bundle, and searching for a title is unreliable because the documents cross-reference each other;
6. merges a full-bleed cover (rendered separately, with no margins and no header or footer) in front of the body.

Mermaid diagrams are bounded to one page each. Without that, a tall flowchart runs past the page edge and is split
mid-figure.

## Contents of the combined document

Proposal · Requirement Traceability · Requirements Checklist · Use Cases · Navigation Structure · Wireframes ·
Flowcharts · Architecture · Database Design · Entity Relationship Diagram · Security Review · Testing ·
Deployment · User Guide · Report Notes · Git Workflow · Team and Responsibilities · Viva Preparation
