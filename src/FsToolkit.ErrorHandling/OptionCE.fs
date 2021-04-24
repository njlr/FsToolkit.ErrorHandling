namespace FsToolkit.ErrorHandling

[<AutoOpen>]
module OptionCE =  
    open System
    type OptionBuilder() =
        member inline this.Return(x) = Some x

        member inline this.ReturnFrom(m: 'T option) = m

        member inline _.Bind(m : Option<'a>, f: 'a -> Option<'b>) = Option.bind f m

        // Could not get Source to work since in loop cases it would match the #seq overload and ask for type annotation
        member this.Bind(m : 'a when 'a:null, f: 'a -> Option<'b>) =  this.Bind(m |> Option.ofObj, f)

        member inline this.Zero() = this.Return ()

        member inline this.Combine(m, f) = Option.bind f m
        member inline this.Combine(m1 : Option<_>, m2 : Option<_>) = this.Bind(m1 , fun () -> m2)

        member inline this.Delay(f: unit -> _) = f

        member inline this.Run(f) = f()

        member inline this.TryWith(m, h) =
            try this.Run m
            with e -> h e

        member inline this.TryFinally(m, compensation) =
            try this.Run m
            finally compensation()

        member inline this.Using
            (resource: 'T when 'T :> IDisposable, binder) : Option<_>=
            this.TryFinally (
                (fun () -> binder resource),
                (fun () -> if not <| obj.ReferenceEquals(resource, null) then resource.Dispose ())
            )

        member this.While
            (guard: unit -> bool, generator: unit -> Option<_>)
            : Option<_> =
            if not <| guard () then this.Zero ()
            else this.Bind(this.Run generator, fun () -> this.While (guard, generator))

        member inline this.For
            (sequence: #seq<'T>, binder: 'T -> Option<_>)
            : Option<_> =
            this.Using(sequence.GetEnumerator (), fun enum ->
                this.While(enum.MoveNext,
                    this.Delay(fun () -> binder enum.Current)))

        /// <summary>
        /// Method lets us transform data types into our internal representation.  This is the identity method to recognize the self type.
        /// 
        /// See https://stackoverflow.com/questions/35286541/why-would-you-use-builder-source-in-a-custom-computation-expression-builder
        /// </summary>
        member inline _.Source(result : Option<_>) : Option<_> = result

                    
    let option = OptionBuilder()

[<AutoOpen>]
// Having members as extensions gives them lower priority in
// overload resolution and allows skipping more type annotations.
module OptionExtensions =
  open System
  type OptionBuilder with
    /// <summary>
    /// Needed to allow `for..in` and `for..do` functionality
    /// </summary>
    member inline __.Source(s: #seq<_>) = s


    // /// <summary>
    // /// Method lets us transform data types into our internal representation.
    // /// </summary>
    member inline this.Source(nullable : Nullable<'a>) : Option<'a> = nullable |> Option.ofNullable


    



