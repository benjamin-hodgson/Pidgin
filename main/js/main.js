$(document).ready(() => {
    expanders();
    tocFilter();
    localNav();
    search();


    if (window.hljs) {
        hljs.highlightAll();
    } else {
        const script = document.getElementById("highlightjs-script");
        script.onload = () => { hljs.highlightAll(); };
    }


    function expanders() {
        $(".js-expander").click(e => {
            const $toggle = $(e.currentTarget);
            const $el = $("#" + $toggle.attr("aria-controls"));
            $toggle.toggleClass("active");
            $el.toggleClass("active");
        });
    }


    function tocFilter() {
        $("#toc-filter").closest("form").submit(e => { e.preventDefault(); });
        $("#toc-filter").on("input", debounce(applyTocFilter));
    
        function applyTocFilter() {
            const term = $("#toc-filter").val().trim();
            if (!term) {
                $("#toc *").removeClass("active");
                $("#toc *").removeClass("hide");
                // active initially
                $("#toc .active-i").addClass("active");
                return;
            }
            $("#toc").find("ul, button").removeClass("active");
            $("#toc li").addClass("hide");
            const $matches = $("#toc a")
                .filter((_, e) => $(e).text().toLowerCase().indexOf(term.toLowerCase()) >= 0);
            if ($matches.length) {
                const $ancestors = $matches.parentsUntil("#toc", "li").add("#toc");
                $ancestors.removeClass("hide");
                $ancestors.children("ul, button").addClass("active");
            }
        };
    }
    

    function localNav() {
        const $localTocLinks = $("#local-toc a");
        const observer = new IntersectionObserver(
            es => {
                for (const e of es) {
                    if (e.isIntersecting) {
                        $localTocLinks.add($("#local-toc ul")).removeClass("active");
                        const $a = $localTocLinks.filter(matchingHrefSelector(e.target));
                        const $ancestors = $a.parentsUntil("#local-toc", "li")
                            .add("#local-toc");
                        $ancestors.children("a, ul").addClass("active");
                    }
                }
            },
            { rootMargin: "0px 0px -66% 0px" }
        );
        $("h1, h2, h3, h4, h5, h6")
            .filter("[id]")
            .filter((_, e) => $localTocLinks.is(matchingHrefSelector(e)))
            .each((_, e) => observer.observe(e));
        
        function matchingHrefSelector(el) {
            return '[href="#' + $(el).attr("id") + '"]';
        }
    }

    
    function search() {
        $("#search").closest("form").submit(e => { e.preventDefault(); });

        let indexReady = false;

        const worker = new Worker('js/search-worker.js');
        worker.onmessage = function (oEvent) {
            switch (oEvent.data.e) {
                case 'index-ready':
                    indexReady = true;
                    performSearch($('#search').val());
                    break;
                case 'query-ready':
                    handleSearchResults(oEvent.data.q, oEvent.data.d);
                    break;
            }
        }

        $('#search').on("input", e => {
            const q = $(e.currentTarget).val();
            if (indexReady) {
                performSearch(q);
            }
        });

        const performSearch = debounce(q => {
            if (q && q.length >= 3) {
                worker.postMessage({ q });
                $('.hide-when-search').hide();
                $('#search-results').show();
            } else {
                $('.hide-when-search').show();
                $('#search-results').hide();
            }
        });

        function handleSearchResults(q, hits) {
            if (hits.length === 0) {
                $('#search-results').html('<p>No results found</p>');
                return;
            }
            const $ul = $('<ul class="search-results-list">');
            $('#search-results').empty().append($ul);
            for (const hit of hits) {
                var itemHref = hit.href;
                var itemTitle = hit.title;
                var itemNode = $('<li>');
                var itemTitleNode = $('<h2>').append($('<a class="xref">').attr('href', itemHref).text(itemTitle));
                var itemBrief = htmlEncode(extractContentBrief(hit.keywords, q));

                const re = new RegExp(
                    "(" + q.split(/\s+/)
                        .filter(word => word.trim().length > 0)
                        .map(word => word.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&'))
                        .join("|") + ")",
                    "ig"
                );
                const itemBriefNode = $("<p>").html(itemBrief.replace(re, match => "<mark>" + match + "</mark>"));

                itemNode.append(itemTitleNode).append(itemBriefNode);
                $ul.append(itemNode);
            }
        }
        
        function extractContentBrief(content, query) {
            var briefOffset = 512;
            var words = query.split(/\s+/g);
            var queryIndex = content.indexOf(words[0]);
            if (queryIndex > briefOffset) {
                return "..." + content.slice(queryIndex - briefOffset, queryIndex + briefOffset) + "...";
            } else if (queryIndex <= briefOffset) {
                return content.slice(0, queryIndex + briefOffset) + "...";
            }
        }
        function htmlEncode(str) {
            return str.replace(/[\u00A0-\u9999<>\&]/gim, i => '&#' + i.charCodeAt(0) + ';');
        }
    };
    

    function debounce(f, ms = 300) {
        let timeout;
        return (...args) => {
            if (timeout) {
                clearTimeout(timeout);
            }
            timeout = setTimeout(() => {
                f(...args);
                timeout = undefined;
            }, ms);
        }
    }
});
