﻿namespace Website

open IntelliFactory.WebSharper
open IntelliFactory.WebSharper.Html
open IntelliFactory.WebSharper.JQuery
open Mongo

module Snippets =

    module Server =
        
        let inline snippetData (snippet : Snippet) =
            snippet.Link, snippet.Title, snippet.Description

        [<RpcAttribute>]
        let latestFSharpSnippets () =
            async {
                return
                    Snippets.latest20()
                    |> Seq.toArray
                    |> Array.map snippetData
            }

        [<RpcAttribute>]
        let snippetsAfterSkip skip =
            async {
                return
                    Mongo.Snippets.skipLatest20 skip
                    |> Seq.toArray
                    |> Array.map snippetData
            }

    module Client =
        
        [<JavaScriptAttribute>]
        let makeSnippetLi link title description =
            LI [Attr.Class "snippet"] -< [
                A [HRef link; Attr.Target "_blank"] -< [
                    Strong [Text title]
                ]
                P [Text description]
            ]

        [<JavaScriptAttribute>]
        let incrementSnippetsCount x =
            Utilities.Client.incrementDataCount "#fsharpSnippets" "data-snippets-count" x

        [<JavaScriptAttribute>]
        let snippetsDiv () =

            let snippetsList = UL [Id "snippetsList"]

            let loadMoreBtn =
                Button [Text "Load More"; Attr.Class "btn loadMore"]
                |>! OnClick (fun x _ ->
                    async {
                        x.SetAttribute("disabled", "disabled")
                        let jquery = JQuery.Of "#fsharpSnippets"
                        let count = jquery.Attr "data-snippets-count" |> int
                        let! fsharpSnippets = Server.snippetsAfterSkip count

                        fsharpSnippets
                        |> Array.map (fun (link, title, description) ->
                            makeSnippetLi link title description)
                        |> Array.iter snippetsList.Append

                        let count' = Array.length fsharpSnippets
                        incrementSnippetsCount count'
                        x.RemoveAttribute("disabled")
                    } |> Async.Start)

//            Div [Id "snippetsDiv"] -< [snippetsList] //; loadMoreBtn]
            Div [Id "fsharpSnippets"; HTML5.Attr.Data "snippets-count" "0"] -< [snippetsList] //; loadMoreBtn]
            |>! OnAfterRender(fun _ ->
                async {
                    let! fsharpSnippets = Server.latestFSharpSnippets ()
                    fsharpSnippets
                    |> Array.map (fun (link, title, description) ->
                        makeSnippetLi link title description)
                    |> Array.iter (fun x -> snippetsList.Append x)
                    incrementSnippetsCount 20
//                    loadMoreBtn.SetCss("visibility", "visible")
                } |> Async.Start)

        type SnippetsViewer () =
            
            inherit Web.Control ()

            [<JavaScriptAttribute>]
            override this.Body = snippetsDiv () :> _
