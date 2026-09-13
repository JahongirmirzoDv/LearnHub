# Vendored client libraries

Served from LearnHub itself (no runtime CDN) so the Content-Security-Policy can stay `'self'`.
All files were taken unmodified from the npm packages below, except that the trailing
`sourceMappingURL` comments were removed from the Bootstrap files because the `.map` files are not shipped.

| Library | Version | Files | Licence |
|---------|---------|-------|---------|
| Bootstrap | 5.3.8 | `bootstrap/css/bootstrap.min.css`, `bootstrap/js/bootstrap.bundle.min.js` | MIT |
| Bootstrap Icons | 1.13.1 | `bootstrap-icons/bootstrap-icons.min.css`, `bootstrap-icons/fonts/bootstrap-icons.woff2` | MIT |
| jQuery | 3.7.1 | `jquery/jquery.min.js` | MIT |
| jQuery Validation | 1.22.1 | `jquery-validation/jquery.validate.min.js` | MIT |
| jQuery Validation Unobtrusive | 4.0.0 | `jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js` | MIT |
| Overpass (variable font, via Fontsource) | 5.3.0 | `overpass/overpass-latin-wght-normal.woff2`, `overpass/overpass-latin-ext-wght-normal.woff2` | SIL OFL 1.1 |

jQuery stays on 3.7.1 because jQuery Validation Unobtrusive 4.0.0 declares `jquery ^3.6.0`.

To update a library, download the same files from `https://cdn.jsdelivr.net/npm/<package>@<version>/…`
and update this table.
