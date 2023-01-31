# -*- coding: utf-8 -*-

import sys
import re

from sphinx import addnodes, version_info
from sphinx.roles import XRefRole
from sphinx.locale import _
from sphinx.domains import Domain, ObjType
from sphinx.directives import ObjectDescription
from sphinx.util.nodes import make_refnode
# ### Next line is deprecated, and it seems to work without it, as per kOS PR #2339 ###
# from sphinx.util.compat import Directive
from sphinx.util.docfields import Field, TypedField

ks_sig_re = re.compile(r'''
    (?:
        (?P<prefix>
            [a-zA-Z][\w:]*
        )
        :
    )?
    (?P<object>
        [a-zA-Z][\w]*
    )
    (?:
        \(
        (?P<args>
            .*
        )
        \)
    )?
''', re.VERBOSE)
ks_keyword_sig_re = re.compile(r'''(?P<object>[a-zA-Z][\w]*)(?:(?:\s+)(?P<params>(?:.+\((?P<args>.+)\))|(?:.+)))?\.?''', re.VERBOSE)

class KOSObject(ObjectDescription):
    def add_target_and_index(self, name, sig, signode):
        targetname = self.objtype + ':' + name.upper()
        if targetname not in self.state.document.ids:
            signode['names'].append(targetname)
            signode['ids'].append(targetname)
            signode['first'] = (not self.names)
            self.state.document.note_explicit_target(signode)

            objects = self.env.domaindata['ks']['objects']
            key = (self.objtype, name.upper())
            if key in objects:
                self.env.warn(self.env.docname,
                              'duplicate description of %s %s, ' %
                              (self.objtype, name.upper()) +
                              'other instance in ' +
                              self.env.doc2path(objects[key]),
                              self.lineno)

            objects[key] = self.env.docname
        indextext = self.get_index_text(self.objtype, name)
        if indextext:
            # sphinx 1.4.0+ requires 5 elements
            if version_info > (1, 4, 0, '', 0):
                self.indexnode['entries'].append(('single', indextext,
                                                  targetname, '', None))
            else:
                self.indexnode['entries'].append(('single', indextext,
                                                  targetname, ''))

class KOSGlobal(KOSObject):
    doc_field_types = [
        Field('access', label=_('Access'), has_arg=False),
        Field('type'  , label=_('Type'  ), has_arg=False),
    ]

    def handle_signature(self, sig, signode):
        fullname = sig
        if sig.upper().startswith('CONSTANT():'):
            name = sig[11:]
        else:
            name = sig
        signode += addnodes.desc_name(name, fullname)
        return name

    def get_index_text(self, objectname, name):
        return _('{}'.format(name))

class KOSFunction(KOSObject):
    doc_field_types = [
        Field('access', label=_('Access'), has_arg=False),
        TypedField('parameter', label=_('Parameters'),
                   names=('param', 'parameter', 'arg', 'argument'),
                   typerolename='struct', typenames=('paramtype', 'type')),
        Field('returnvalue', label=_('Returns'), has_arg=False,
              names=('returns', 'return')),
        Field('returntype', label=_('Return type'), has_arg=False,
              names=('rtype','type')),
    ]

    def handle_signature(self, sig, signode):
        m = ks_sig_re.match(sig)
        name = m.group('object')
        signode += addnodes.desc_name(name,name)

        args = m.group('args')
        if args:
            signode += addnodes.desc_parameterlist(args,args)
        else:
            signode += addnodes.desc_parameterlist()
        return name

    def get_index_text(self, objectname, name):
        return _('{}()'.format(name))


class KOSKeyword(KOSObject):
    doc_field_types = [
        TypedField('parameter', label=_('Parameters'),
                   names=('param', 'parameter', 'arg', 'argument'),
                   typerolename='struct', typenames=('paramtype', 'type')),
    ]

    def handle_signature(self, sig, signode):
        m = ks_keyword_sig_re.match(sig)
        name = m.group('object')
        signode += addnodes.desc_name(sig,sig)
        return name

    def get_index_text(self, objectname, name):
        return _('{}'.format(name))

class KOSStructure(KOSObject):
    def handle_signature(self, sig, signode):
        m = ks_sig_re.match(sig)
        name = m.group('object')
        signode += addnodes.desc_annotation('structure ','structure ')
        signode += addnodes.desc_name(name,name)
        return name

    def get_index_text(self, objectname, name):
        return _('{} [struct]'.format(name))

    def before_content(self):
        self.env.temp_data['ks:structure'] = self.names[0]

    def after_content(self):
        self.env.temp_data['ks:structure'] = None

class KOSAttribute(KOSObject):

    doc_field_types = [
        Field('access', label=_('Access'), has_arg=False),
        Field('type'  , label=_('Type'  ), has_arg=False),
    ]

    def handle_signature(self, sig, signode):
        m = ks_sig_re.match(sig)
        name = m.group('object')
        struct = None  #might override further down

        current_struct = self.env.temp_data.get('ks:structure')
        if m.group('prefix') is None:
            if current_struct is not None:
                struct = current_struct
                fullname = current_struct + ':' + name
            else:
                raise Exception("Attribute name lacks a prefix and isn't " +
                    "indented inside a structure section.  Problem " +
                    "occurred at " + self.env.docname + ", line " +
                     str(self.lineno) +
                     ".")
        else:
            struct = m.group('prefix').split(':')[-1]
            fullname = struct + ':' + name

        if struct is not None:
            if struct != '':
                signode += addnodes.desc_type(struct,struct+':')
        signode += addnodes.desc_name(fullname, name)

        return fullname

    def get_index_text(self, objectname, name):
        return _('{}'.format(name))

class KOSMethod(KOSObject):

    doc_field_types = [
        Field('access', label=_('Access'), has_arg=False),
        TypedField('parameter', label=_('Parameters'),
                   names=('param', 'parameter', 'arg', 'argument'),
                   typerolename='obj', typenames=('paramtype', 'type')),
        Field('returnvalue', label=_('Returns'), has_arg=False,
              names=('returns', 'return')),
        Field('returntype', label=_('Return type'), has_arg=False,
              names=('rtype','type')),
    ]

    def handle_signature(self, sig, signode):
        m = ks_sig_re.match(sig)
        name = m.group('object')
        struct = None  #might override further down

        current_struct = self.env.temp_data.get('ks:structure')
        if m.group('prefix') is None:
            if current_struct is not None:
                struct = current_struct
                fullname = current_struct + ':' + name
            else:
                raise Exception("Method name lacks a prefix and isn't " +
                    "indented inside a structure section.  Problem " +
                    "occurred at " + self.env.docname + ", line " +
                     str(self.lineno) +
                     ".")
        else:
            struct = m.group('prefix').split(':')[-1]
            fullname = struct + ':' + name

        if struct is not None:
            if struct != '':
                signode += addnodes.desc_type(struct,struct+':')

        signode += addnodes.desc_name(fullname, name)

        args = m.group('args')
        if args:
            signode += addnodes.desc_parameterlist(args,args)
        else:
            signode += addnodes.desc_parameterlist()


        return fullname

    def get_index_text(self, objectname, name):
        return _('{}()'.format(name))

class KOSXRefRole(XRefRole):

    def process_link(self, *args):
        title, target =  super(KOSXRefRole,self).process_link(*args)
        m = ks_sig_re.match(target)
        target = m.group('object')
        if m.group('prefix') is not None:
            struct = m.group('prefix').split(':')[-1]
            target = ':'.join([struct,target])
        return title, target.upper()

class KOSAttrXRefRole(XRefRole):

    def process_link(self, env, *args):
        title, target =  super(KOSAttrXRefRole,self).process_link(env, *args)
        m = ks_sig_re.match(target)
        target = m.group('object')
        if m.group('prefix') is None:
            current_struct = env.temp_data.get('ks:structure')
            if current_struct is not None:
                target = ':'.join([current_struct,target])
        else:
            struct = m.group('prefix').split(':')[-1]
            target = ':'.join([struct,target])
        return title, target.upper()

class KOSDomain(Domain):
    name = 'ks'
    label = 'KerboScript'
    initial_data = {
        'objects': {},  # fullname -> docname, objtype
    }

    object_types = {
        'global'   : ObjType(_('global'   ), 'global'),
        'function' : ObjType(_('function' ), 'func'  ),
        'keyword'  : ObjType(_('keyword'  ), 'key'   ),
        'structure': ObjType(_('structure'), 'struct'),
        'attribute': ObjType(_('attribure'), 'attr'  ),
        'method'   : ObjType(_('method'   ), 'meth'  ),
    }
    directives = {
        'global'   : KOSGlobal,
        'function' : KOSFunction,
        'keyword'  : KOSKeyword,
        'structure': KOSStructure,
        'attribute': KOSAttribute,
        'method'   : KOSMethod,
    }
    roles = {
        'global': KOSXRefRole(),
        'func'  : KOSXRefRole(),
        'key'   : KOSXRefRole(),
        'struct': KOSXRefRole(),
        'attr'  : KOSAttrXRefRole(),
        'meth'  : KOSAttrXRefRole(),
    }

    def resolve_xref(self, env, fromdocname, builder, typ, target, node,
                     contnode):
        objects = self.data['objects']
        objtypes = self.objtypes_for_role(typ)
        for objtype in objtypes:
            if (objtype, target.upper()) in objects:
                return make_refnode(builder, fromdocname,
                                    objects[objtype, target],
                                    objtype + ':' + target.upper(),
                                    contnode, target + ' ' + objtype)

    def get_objects(self):
        # iteritems() was renamed to items() in python 3
        if sys.version_info[0] < 3:
            for (typ, name), docname in self.data['objects'].iteritems():
                yield name, name, typ, docname, name, 1
        else:
            for (typ, name), docname in self.data['objects'].items():
                yield name, name, typ, docname, name, 1

def setup(app):
    app.add_domain(KOSDomain)
